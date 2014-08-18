/* This is the plugin for the µDelta from eMotion Tech
 * It was originaly just a prototype in order to see if it works or not
 * but under the constraint of time we transformed it into a public version.
 * So sadly it is still a demo version, and really really rough and hardcoded!!
 * The prototype was coded in a week and I couldn't spend more time until now...
 * Please be carefull with your eyes, you may bleed.
 * 
 * 
 * I may add some features and code clarification as:
 * - Add some more comments
 * - refactoring everything, make it clear
 * - Add some configuration file for a general Delta purpose (and not just the µDelta)
 * - internationalization
 * - Some more expert mode features (as special auto-configuration, or serial-number based auto-update)
 * 
 * 
 * @Repetier: if you read this, thanks for your great work!! 
 * Plugin possibility is really really cool for any user! Please keep your host free!
 * 
 * Authors: Hugo FLYE and for the graphic: Antony SOURY (à la vie)
 * Licence: CC-BY-NC-SA 
 */



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RepetierHostExtender.interfaces;
using System.Diagnostics;
using System.Globalization;

using System.Threading;
using System.Net;

namespace uDeltaPlugin
{
    public partial class uDeltaControl : UserControl,IHostComponent
    {
        private IHost host;
        public uDeltaControl()
        {
            InitializeComponent();
        }

        class EEpromValues
        {
            public float Zmax;
            public float[] Tower;
            public bool initialised;
            public EEpromValues()
            {
                Zmax = 0;
                Tower = new float[]{0,0,0};
                initialised = false ;
            }
        }

        public void ComponentActivated()
        {
       
        }

        public RepetierHostExtender.geom.ThreeDView Associated3DView
        {
            get;
            set;
        }
          //public  typeof RepetierHostExtender.interfaces.IHostComponent.Associated3DView Associated3DView;
        
        // Some patch group for animations
        private System.Windows.Forms.PictureBox[] padT;
        private System.Windows.Forms.PictureBox[] padTC;
        private System.Windows.Forms.PictureBox[] padTH;
        private System.Windows.Forms.PictureBox[] padTUD;
        private System.Windows.Forms.PictureBox[] padTUDH;
        private System.Windows.Forms.PictureBox[] padTUDN;
        private System.Windows.Forms.PictureBox[][] ConnectedButtons;
        private System.Windows.Forms.PictureBox[][] UnconnectedButtons;


        private bool[] towerValidation;
        private bool flag_towerCalibration;
        private float[] towerValue;
        private int EEPROM_flag;
        private EEpromValues EEpromValue;
        private int towerLastSelection;
        private float lastZmaxValue;
        private String TowerLastText;
        /// <summary>
        /// Store reference to host for later use
        /// </summary>
        /// <param name="_host">Host instance</param>
        public void Connect(IHost _host)
        {
            host = _host;
            EEpromValue = new EEpromValues();

            // Initialize all pad storage for animations 
            padT = new System.Windows.Forms.PictureBox[] { padT1, padT2, padT3 };
            padTC = new System.Windows.Forms.PictureBox[] { padTH1, padTH2, padTH3 };
            padTH = new System.Windows.Forms.PictureBox[] { padTC1, padTC2, padTC3 };
            padTUD = new System.Windows.Forms.PictureBox[] { padZU1, padZU05,padZD05, padZD1};
            padTUDH = new System.Windows.Forms.PictureBox[] { padZUH1, padZUH05, padZDH05, padZDH1 };
            padTUDN = new System.Windows.Forms.PictureBox[] { padTLU1, padTLU05, padTLD05, padTLD1 };
            ConnectedButtons = new System.Windows.Forms.PictureBox[][] { new System.Windows.Forms.PictureBox[] { pad_extrude, pad_retire }, new System.Windows.Forms.PictureBox[] { padI_extrude, padI_retire, padH_extrude,padH_retire } };
            UnconnectedButtons = new System.Windows.Forms.PictureBox[][] { new System.Windows.Forms.PictureBox[] { padI_extrude, padI_retire }, new System.Windows.Forms.PictureBox[] { padH_extrude, padH_retire, pad_extrude, pad_retire } };
 
            towerValidation = new bool[] { false, false, false };
            
            EEPROM_flag = 0;
            towerLastSelection = 0;
            flag_towerCalibration = false;
            TowerLastText = "Connectez votre imprimante";

            //Add the connection change event
            host.Connection.eventConnectionChange += Connection_eventConnectionChange;
        }
        #region IHostComponent implementation

        // Name inside component repository
        public string ComponentName { get { return "µDeltaPlugin"; } }
        // Name for tab
        public string ComponentDescription { get { return "µDelta"; } }
        // Used for positioning.
        public int ComponentOrder { get { return 8000; } }
        // Where to add it. We want it on the right tab.
        public PreferredComponentPositions PreferredPosition { get { return PreferredComponentPositions.SIDEBAR; } }
        // Return the UserControl.
        public Control ComponentControl { get { return this; } }

        #endregion

        #region Std functions

        private void TowerSetText(string msg)
        {
            LabelTowerInformation.Text = msg;
            TowerLastText = msg;

        }

        private void TowerRestoreText()
        {
            LabelTowerInformation.Text = TowerLastText;
        }

        //Change temporary the instruction 
        private void TowerTemporaryText(string msg)
        {
            LabelTowerInformation.Text = msg;
        }

        //Request a EEPROM update
        private void updateHostEEPROMValues()
        {
            host.Connection.eventResponse += Connection_eventResponseEEPROM;
            host.Connection.injectManualCommand("M205");
        }

        //Visual effect and management of selected tower
        private void tower_clickSelection(int num)
        {
            towerValidation[num] = true;

            if (towerLastSelection > 0) //validate last tower
            {
                towerValue[towerLastSelection - 1] = EEpromValue.Zmax-host.Connection.Analyzer.RealZ; // save tower value
            }
            for (int i = 0; i < padT.Length; i++)
            {
                if (num != i)
                {
                    if (towerValidation[i]) //Tower validated
                    {
                        padT[i].Visible = false;
                        padTC[i].Visible = true;
                    }
                }
            }

            // Visual feedback on clicking
            padT[num].Visible = false;
            //padTH[num].Visible = true;
            padTC[num].Visible = true;


            //Record selection for next validation
            towerLastSelection = num + 1;

            //Check if all pad are activated or clicked in order to let the user click on the save button
            if ((padTH[0].Visible || padTC[0].Visible) && (padTH[1].Visible || padTC[1].Visible) && (padTH[2].Visible || padTC[2].Visible))
            {
                TowerSave.Visible = true;
                TowerSaveH.Visible = false;
                TowerSaveI.Visible = false;
                TowerSetText("Vous avez terminé? N'oubliez pas de sauver la calibration");
            }
            else
            {
                //Informations
                TowerSetText("Descendez la tête avec les flèches jusqu'a toucher la feuille de papier, changez de tour quand vous avez terminé");
                
            }
        }

        // Visual effect for the tower button
        private void TowerButton_enter_leave(int num, bool activity)
        {
            if(activity) {
                padTH[num].Visible = true;
                padT[num].Visible = false;
                padTC[num].Visible = false;
                TowerTemporaryText("Selectionnez la tour");
            }
            else
            {
                if (towerValidation[num])
                {
                    padTC[num].Visible = true;
                    padT[num].Visible = false;
                }
                else
                {
                    padT[num].Visible = true;
                    padTC[num].Visible = false;
                }
                padTH[num].Visible = false;        
                TowerRestoreText();
            }
            

        }

        // Visual effect Z button
        private void TowerZButtonSelection(int num,int type)
        {
            if(type == 0) {
                padTUD[num].Visible = true;
                padTUDH[num].Visible = false;
                padTUDN[num].Visible = false;
            }
            else if(type == 1){
                padTUDH[num].Visible = true;
                padTUD[num].Visible = false;
                padTUDN[num].Visible = false;
            }
            else 
            {
                padTUDN[num].Visible = true;
                padTUD[num].Visible = false;
                padTUDH[num].Visible = false;
            }
        }
        // Visual effect 
        private void cleanTowerSelection()
        {
            for (int i = 0; i < padT.Length; i++)
            {
                padT[i].Visible = true;
                padTC[i].Visible = false;
                padTH[i].Visible = false;
            }
        }
        private void relativeMove(float move)
        {
            if (EEpromValue.initialised == false)
            {
                updateHostEEPROMValues();
            }
            if (move > 0)
            {
                host.Connection.injectManualCommand("G1 Z" + (host.Connection.Analyzer.RealZ + move).ToString().Replace(',', '.'));
            }
            else
            {
                float tempPosition = host.Connection.Analyzer.RealZ + move;
                host.LogMessage("Realz :" + host.Connection.Analyzer.RealZ.ToString()+" Move:"+move.ToString()+" R:"+tempPosition.ToString());
                if (tempPosition < 0) //Position under the maximum length
                {
                    host.LogWarning("you should not be under 200mm, check your Zmax in EEPROM");
                }
                else //Position under limits
                    host.Connection.injectManualCommand("G1 Z" + (host.Connection.Analyzer.RealZ + move).ToString().Replace(',', '.'));
            }
        }
        private void sendTowerSendToEEPROM(int index, float value)
        {
            if (index == 0)
                host.Connection.injectManualCommand("M206 T1 S" + value.ToString().Replace(',', '.') + " P893");
            if (index == 1)
                host.Connection.injectManualCommand("M206 T1 S" + value.ToString().Replace(',', '.') + " P895");
            if (index == 2)
                host.Connection.injectManualCommand("M206 T1 S" + value.ToString().Replace(',', '.') + " P897");
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.reprap-france.com/"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ConnectionInformationExeption()
        {
            TowerTemporaryText("Attention vous n'êtes pas connecté à votre imprimante!!");
            
        }

        private void sendFileToGcode(string path) {
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(path);//@"c:\\eeprom_udelta.emtc");
            while ((line = file.ReadLine()) != null)
            {
                //Print log
                if (line.Contains("LOG"))
                {
                    host.LogWarning(line);
                }
                else //or send commands
                    host.Connection.injectManualCommand(line);
            }

            file.Close();
            //Update plugin EEPROM Settings
            host.Connection.eventResponse += Connection_eventResponseEEPROM;
        }
        #endregion

        #region Evenements


        void Connection_eventConnectionChange(string msg)
        {
            //host.LogWarning("host.Connection.connector.IsConnected():" + host.Connection.connector.IsConnected().ToString());
            if (!host.Connection.connector.IsConnected())
            {
                for (int i = 0; i < UnconnectedButtons[0].Length; i++)
                    UnconnectedButtons[0][i].Visible = true;

                for (int i = 0; i < UnconnectedButtons[1].Length; i++)
                    UnconnectedButtons[1][i].Visible = false;
                //host.LogWarning("Calibration aborted: Connection reseted during calibration");
                TowerSetText("Connectez votre imprimante");

                //EEPROM Net update
                padNetUpdateI.Visible = true;
                padNetUpdateH.Visible = false;
                padNetUpdate.Visible = false;
            }
            else
            {
                for(int i=0;i<ConnectedButtons[0].Length;i++)
                    ConnectedButtons[0][i].Visible = true;
                  
                for(int i=0;i<ConnectedButtons[1].Length;i++)
                    ConnectedButtons[1][i].Visible = false;

                TowerSetText("Cliquez sur le triange pour demarrer la calibration des tours");

                //EEPROM Net update
                padNetUpdate.Visible = true;
                padNetUpdateI.Visible = false;
                padNetUpdateH.Visible = false;
                
            }
        }


        void Connection_eventResponseEEPROM(string response, ref RepetierHostExtender.basic.LogLevel level)
        {
            
            if (response.Contains("Z max"))
            {
                float.TryParse(response.Split(' ')[2], NumberStyles.Float, CultureInfo.InvariantCulture, out EEpromValue.Zmax);

                EEPROM_flag++;
            }
            else if (response.Contains("Tower X"))
            {
                float.TryParse(response.Split(' ')[2], NumberStyles.Float, CultureInfo.InvariantCulture, out EEpromValue.Tower[0]);
                EEPROM_flag++;
            }
            else if (response.Contains("Tower Y"))
            {
                float.TryParse(response.Split(' ')[2], NumberStyles.Float, CultureInfo.InvariantCulture, out EEpromValue.Tower[1]);
                EEPROM_flag++;
            }
            else if (response.Contains("Tower Z"))
            {
                float.TryParse(response.Split(' ')[2], NumberStyles.Float, CultureInfo.InvariantCulture, out EEpromValue.Tower[2]);
                EEPROM_flag++;
            }

            //Stop reccord
            if (EEPROM_flag >= 4)
            {
                EEPROM_flag = 0;
                //Remove event
                host.Connection.eventResponse -= Connection_eventResponseEEPROM;
                EEpromValue.initialised = true;
                }
            //Zmax update on advanced settings
            textZmax.Text = EEpromValue.Zmax.ToString();


        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {

            LabelNetUpdate.Text = "Download done";
            //Read file and uploads commands
            sendFileToGcode(host.WorkingDirectory+"\\eeprom_udelta.emtc");
            LabelNetUpdate.Text = "Mise à jour effectuée";
        }

        void eventFileOk(object sender, CancelEventArgs e)
        {
            host.LogWarning(openFileDialogEEPROMUpdate.FileName);
            sendFileToGcode(openFileDialogEEPROMUpdate.FileName);
            openFileDialogEEPROMUpdate.FileOk -= eventFileOk;
        }


        #endregion
        #region Button functions




        private void ReprapFranceLogo_click(object sender, EventArgs e)
        {
            Process.Start("http://www.reprap-france.com");
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
 
        }


        // UNLOAD FILAMENT
        private void labelRetirer_Click(object sender, EventArgs e)
        {
            
            if (host.Connection.connector.IsConnected() )
            {
                if (host.Connection.connector.InjectedCommands == 0)
                {

                    //100+140+100... : Override extrusion protection on some firmware (you can also remove the protection directly in the firmware)
                    
                    host.Connection.injectManualCommand("M109 S200");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E-10 F100");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E-140 F600");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E-100 F600");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E-130 F600");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E-100 F600");
                }
                else
                    TowerTemporaryText("Soyez patient, attendez la chauffe de l'extruder");
                
            }
        }

        // LOAD FILAMENT
        private void labelExtruder_Click(object sender, EventArgs e)
        {
            if (host.Connection.connector.IsConnected())
            {
                if (host.Connection.connector.InjectedCommands == 0)
                {
                    host.Connection.injectManualCommand("M109 S200");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E100 F600");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E100 F600");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E100 F600");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E100 F600");
                    host.Connection.injectManualCommand("G92 E0");
                    host.Connection.injectManualCommand("G1 E50 F80");
                }
                else
                    TowerTemporaryText("Soyez patient, attendez la chauffe de l'extruder");
            }
        }
        private void padEnter_retire(object sender, EventArgs e)
        {
            padH_retire.Visible = true;
            pad_retire.Visible = false;
            TowerTemporaryText("Retirez le filament de la tête d'extrusion");
        }

        private void padEnter_extrude(object sender, EventArgs e)
        {
            padH_extrude.Visible = true;
            pad_extrude.Visible = false;
            TowerTemporaryText("Entrainez le filament dans la tête d'extrusion");

        }

        private void padLeave_retire(object sender, EventArgs e)
        {
            pad_retire.Visible = true;
            padH_retire.Visible = false;
            TowerRestoreText();
        }

        private void padLeave_extrude(object sender, EventArgs e)
        {
            pad_extrude.Visible = true;
            padH_extrude.Visible = false;
            TowerRestoreText();
        }


        // PAD TOWER +1 +0.05 -0.05 -1


        private void Up1Tower_click(object sender, EventArgs e)
        {
            relativeMove(1);
        }

        private void Up05Tower_click(object sender, EventArgs e)
        {
            relativeMove((float)0.05);
        }

        private void Down05Tower_click(object sender, EventArgs e)
        {
            relativeMove((float)-0.05);
        }

        private void Down1Tower_click(object sender, EventArgs e)
        {
            relativeMove(-1);
        }
        private void padZU1_MouseEnter(object sender, EventArgs e)
        {
            TowerZButtonSelection(0, 1);
            TowerTemporaryText("Remonter la tête de 1 mm");
        }

        private void padZU05_MouseEnter(object sender, EventArgs e)
        {
            TowerZButtonSelection(1, 1);
            TowerTemporaryText("Remonter la tête de 0.05 mm");
        }

        private void padZD05_MouseEnter(object sender, EventArgs e)
        {
            TowerZButtonSelection(2, 1);
            TowerTemporaryText("Descendre la tête de 0.05 mm");
        }

        private void padZD1_MouseEnter(object sender, EventArgs e)
        {
            TowerZButtonSelection(3, 1);
            TowerTemporaryText("Descendre la tête de 1 mm");
        }

        private void padZUH1_MouseLeave(object sender, EventArgs e)
        {
            TowerZButtonSelection(0, 0);
            TowerRestoreText();
        }

        private void padZUH05_MouseLeave(object sender, EventArgs e)
        {
            TowerZButtonSelection(1, 0);
            TowerRestoreText();
        }

        private void padZDH05_MouseLeave(object sender, EventArgs e)
        {
            TowerZButtonSelection(2, 0);
            TowerRestoreText();
        }

        private void padZDH1_MouseLeave(object sender, EventArgs e)
        {
            TowerZButtonSelection(3, 0);
            TowerRestoreText();
        }

        private void Up1Tower_click(object sender, MouseEventArgs e)
        {

        }

        private void Up05Tower_click(object sender, MouseEventArgs e)
        {

        }

        private void Down05Tower_click(object sender, MouseEventArgs e)
        {

        }

        private void Down1Tower_click(object sender, MouseEventArgs e)
        {

        }



        // PAD TOWER POINTS
        private void tower2_click(object sender, EventArgs e)
        {

        }

        private void tower3_click(object sender, EventArgs e)
        {

        }

        private void TowerSendValues_click(object sender, EventArgs e)
        {

        }

        private void UpdateEEPROMFromNet_click(object sender, EventArgs e)
        {

        }




        private void padTower1_click(object sender, EventArgs e)
        {
            if (flag_towerCalibration)
            {
                tower_clickSelection(0);
                if (host.Connection.connector.IsConnected())
                {
                    host.Connection.injectManualCommand("G1 X-43 Y-25 Z30");
                }
                else
                    host.LogWarning("Host not connected, please connect the host before sending commands");


            }

        }

        private void padTower2_click(object sender, EventArgs e)
        {
               if (flag_towerCalibration)
            {
                tower_clickSelection(1);
                if (host.Connection.connector.IsConnected())
                {
                    host.Connection.injectManualCommand("G1 X43 Y-25 Z30");
                }
                else
                    host.LogWarning("Host not connected, please connect the host before sending commands");
            }

        }

        private void padTower3_click(object sender, EventArgs e)
        {
            if (flag_towerCalibration)
            {
                tower_clickSelection(2);
                if (host.Connection.connector.IsConnected())
                {
                    host.Connection.injectManualCommand("G1 X0 Y55 Z30");
                }
                else
                    host.LogWarning("Host not connected, please connect the host before sending commands");
            }
        }

        private void plugin_Loaded(object sender, EventArgs e)
        {
            //Check if connection is active or not 
            //$$ changer ca en le mettant dans les propriétées de connection, aucun interet de mettre un flag biz H
            /*
            {
                flagEventConnectionChangeLoaded = true;

            }
            else
                Connection_eventConnectionChange("reload");
             * */
            updateHostEEPROMValues();

            //Update Z height in the advanced settings
            heightCalibrationText.Text = (EEpromValue.Zmax - host.Connection.Analyzer.RealZ).ToString();
        }

        private void startCalibration_click(object sender, EventArgs e)
        {

            if (host.Connection.connector.IsConnected())
            {

                //Init vars:
                towerValidation = new bool[] { false, false, false };
                towerValue = new float[] { 0, 0, 0 };
                towerLastSelection = 0;


                //Remove Tower Pad cover
                TowerPadCover.Visible = false;

                //Clean interface
                cleanTowerSelection();
                for (int i = 0; i < padTUDN.Length; i++)
                    TowerZButtonSelection(i, 0);

                TowerStop.Visible = true;

                //Clean Tower values
                sendTowerSendToEEPROM(0, 0);
                sendTowerSendToEEPROM(1, 0);
                sendTowerSendToEEPROM(2, 0);

                //Save Zmax
                lastZmaxValue = EEpromValue.Zmax;
                //host.LogWarning("lastZmaxValue" + lastZmaxValue.ToString());
                //Update EEPROM to Zmax = 210
                host.Connection.injectManualCommand("M206 T3 X210 P153");

                //Home under new conditions
                host.Connection.injectManualCommand("G28");
                //Get EEPROM Settings
                updateHostEEPROMValues();
                flag_towerCalibration = true;
                TowerSetText("Choisissez une des tours pour la calibrer");
                
            }
            else
            {
                ConnectionInformationExeption();
            }
            

        }


        private void saveTowerCalibration_click(object sender, EventArgs e)
        {
            //--Save last value
            towerValidation[towerLastSelection - 1] = true;
            towerValue[towerLastSelection - 1] = EEpromValue.Zmax-host.Connection.Analyzer.RealZ; // save tower value


            //--Send to EEPROM
            host.LogMessage("tower: " + towerValue[0].ToString() + " " + towerValue[1].ToString() + " " + towerValue[2].ToString());

            int[] p = new[] { 0, 1, 2 };
            float[] tower2 = new float[] { 0, 0, 0 };
            towerValue.CopyTo(tower2, 0);
            Array.Sort(tower2, p);
            //host.LogMessage("sort: " + p[0].ToString() + " " + p[1].ToString() + " " + p[2].ToString());
            //host.LogMessage("sorted: " + towerValue[p[0]].ToString() + " " + towerValue[p[1]].ToString() + " " + towerValue[p[2]].ToString());
            //Send Zmax
            host.Connection.injectManualCommand("M206 T3 X" + towerValue[p[0]].ToString().Replace(',', '.') + " P153");
            host.LogMessage("Zmax: " + towerValue[p[0]].ToString());
            host.Connection.Analyzer.printerHeight = towerValue[p[0]];
            //Send low tower 0
            sendTowerSendToEEPROM(p[0], 0);
            sendTowerSendToEEPROM(p[1], (towerValue[p[1]] - towerValue[p[0]]) * 80);
            sendTowerSendToEEPROM(p[2], (towerValue[p[2]] - towerValue[p[0]]) * 80);

            //Put the Towering GUI in inactive position
            cleanTowerSelection();
            flag_towerCalibration = false;
            for (int i = 0; i < padTUDN.Length; i++)
                TowerZButtonSelection(i, 2);

            //Inactive mode for the saving buttun
            TowerSaveI.Visible = true;
            TowerSaveH.Visible = false;
            TowerSave.Visible = false;
            TowerStop.Visible = false;
            towerLastSelection = 0;
            //Remove Tower Pad cover
            TowerPadCover.Visible = true;
            TowerSetText("Cliquez sur le triangle pour démarrer la calibration");


        }
        private void TowerAvortCalibration(object sender, EventArgs e)
        {

            //Load last Zmax
            //host.LogWarning("lastZmaxValue" + lastZmaxValue.ToString());
            host.Connection.injectManualCommand("M206 T3 X" + lastZmaxValue.ToString().Replace(',', '.') + " P153");
            //Put the Towering GUI in inactive position
            cleanTowerSelection();
            flag_towerCalibration = false;
            for (int i = 0; i < padTUDN.Length; i++)
                TowerZButtonSelection(i, 2);
            towerLastSelection = 0;
            //Inactive mode for the saving buttun
            TowerSaveI.Visible = true;
            TowerSaveH.Visible = false;
            TowerSave.Visible = false;
            TowerStop.Visible = false;
            //Remove Tower Pad cover
            TowerPadCover.Visible = true;
            TowerSetText("Cliquez sur le triangle pour démarrer la calibration");
            updateHostEEPROMValues();
        }
        private void ExpertMode_Checked(object sender, EventArgs e)
        {
            if (checkBoxExpertMode.Checked)
            {
                groupBoxPIDTune.Visible = true;
                groupBoxZCalibration.Visible = true;
                logoDelta.Top += 180;
                logoEMT.Top += 180;
                buttonUpdateFromFile.Visible = true;
            }
            else
            {
                groupBoxPIDTune.Visible = false;
                groupBoxZCalibration.Visible = false;
                buttonUpdateFromFile.Visible = false;
                logoDelta.Top -= 180;
                logoEMT.Top -= 180;
            }
        }

        private void udeltaLogo_click(object sender, EventArgs e)
        {
            Process.Start("http://www.reprap-france.com");
        }

        private void TowerSave_enter(object sender, EventArgs e)
        {
            if (flag_towerCalibration)
            {
                TowerSaveH.Visible = true;
                TowerSaveI.Visible = false;
                TowerSave.Visible = false;
                TowerTemporaryText("Sauver la configuration?");
            }

        }

        private void TowerSave_Leave(object sender, EventArgs e)
        {
            if (flag_towerCalibration)
            {
                TowerSave.Visible = true;
                TowerSaveH.Visible = false;
                TowerSaveI.Visible = false;
                TowerRestoreText();
            }
            
        }

       

        private void TowerCover_enter(object sender, EventArgs e)
        {
            if (host.Connection.connector.IsConnected())
                TowerTemporaryText("Demarrer la calibration?");
        }

        private void EEpromNetUpdate_enter(object sender, EventArgs e)
        {
            padNetUpdateH.Visible = true;
            padNetUpdate.Visible = false;
        }
        private void NetUpdate_Leave(object sender, EventArgs e)
        {
            padNetUpdate.Visible = true;
            padNetUpdateH.Visible = false;
        
        }

        private void NetUpdate_click(object sender, EventArgs e)
        {
            if (host.Connection.connector.IsConnected())
            {
                if (CheckForInternetConnection())
                {
                    LabelNetUpdate.Text = "Downloading";
                    WebClient webClient = new WebClient();
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                    webClient.DownloadFileAsync(new Uri("http://www.reprap-france.com/download/eeprom_udelta.emtc"), host.WorkingDirectory+"\\eeprom_udelta.emtc");

                }
                else
                {
                    LabelNetUpdate.Text = "Connection error";
                }
            }
            else
                LabelNetUpdate.Text = "Connectez votre imprimante";

        }

        private void inactiveTryToPress_click(object sender, EventArgs e)
        {
            if(!host.Connection.connector.IsConnected())
                ConnectionInformationExeption();
        }


        private void TowerAbort_Enter(object sender, EventArgs e)
        {
            TowerTemporaryText("Annuler la calibration");
            
        }
        #endregion

        private void TowerAbort_Leave(object sender, EventArgs e)
        {
            TowerRestoreText();
        }

        private void TowerCover_leave(object sender, EventArgs e)
        {
            TowerRestoreText();
        }

        private void padT1_enter(object sender, EventArgs e)
        {
            TowerButton_enter_leave(0, true);
        }

        private void padT2_enter(object sender, EventArgs e)
        {
            TowerButton_enter_leave(1, true);
        }

        private void padT3_enter(object sender, EventArgs e)
        {
            TowerButton_enter_leave(2, true);
        }

        private void padT1_leave(object sender, EventArgs e)
        {
            TowerButton_enter_leave(0, false);
        }

        private void padT2_leave(object sender, EventArgs e)
        {
            TowerButton_enter_leave(1, false);
        }

        private void padT3_leave(object sender, EventArgs e)
        {
            TowerButton_enter_leave(2, false);
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialogEEPROMUpdate.FileOk += eventFileOk;
            openFileDialogEEPROMUpdate.ShowDialog();
       
           
        }

        private void buttonGoZ_Click(object sender, EventArgs e)
        {
            float position;
            if (host.Connection.connector.IsConnected())
            {

                float.TryParse(heightCalibrationText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out position);
                position = EEpromValue.Zmax - position;
                if (position > 0)
                {
                    host.Connection.injectManualCommand("G1 Z" + position.ToString().Replace(',', '.'));
                }
                else
                    host.Connection.injectManualCommand("G1 Z0");
        
            }
        }

        private void buttonDefHeight_Click(object sender, EventArgs e)
        {
            if (host.Connection.connector.IsConnected())
            {
                //Update EEPROM
                host.Connection.injectManualCommand("M206 T3 X" + heightCalibrationText.Text.Replace(',', '.') + " P153");
                //Update Zmax value in plugin
                float.TryParse(heightCalibrationText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out  EEpromValue.Zmax);
                textZmax.Text = heightCalibrationText.Text;
            }
        }

        private void button_tune_PID_Click(object sender, EventArgs e)
        {
            if (host.Connection.connector.IsConnected())
            {
                float tmp = 0;
                float.TryParse(PID_textbox.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out tmp);
                if (tmp > 100 && tmp < 300)
                {

                    host.Connection.injectManualCommand("M106 s255");
                    host.Connection.injectManualCommand("M303 E0 S" + tmp.ToString() + " C8 X0");
                    host.Connection.injectManualCommand("M500");
                }
                else
                    TowerTemporaryText("Erreur: valeur de text PID");
            }
        }

        private void button_default_PID_Click(object sender, EventArgs e)
        {
            if (host.Connection.connector.IsConnected())
            {
                host.Connection.injectManualCommand("M206 T3 X7 P218");
                host.Connection.injectManualCommand("M206 T3 X2 P222");
                host.Connection.injectManualCommand("M206 T3 X40 P226");
            }
        }

        private void Home_click(object sender, EventArgs e)
        {
            if (host.Connection.connector.IsConnected())
            {
                host.Connection.injectManualCommand("G28");
            }
        }

   
 
 


    }
}
