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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RepetierHostExtender.interfaces;

namespace uDeltaPlugin
{
    public class uDeltaPlugin : IHostPlugin
    {
        IHost host;
        /// <summary>
        /// Called first to allow filling some lists. Host is not fully set up at that moment.
        /// </summary>
        /// <param name="host"></param>
        public void PreInitalize(IHost _host)
        {
            host = _host;
        }
        /// <summary>
        /// Called after everything is initalized to finish parts, that rely on other initializations.
        /// Here you must create and register new Controls and Windows.
        /// </summary>
        public void PostInitialize()
        {
            // Add the CoolControl to the right tab
           uDeltaControl cool = new uDeltaControl();
            cool.Connect(host);
            host.RegisterHostComponent(cool);

            // Add some text in the about dialog
            host.AboutDialog.RegisterThirdParty("µDeltaPlugin", "\r\n\r\nµDelta Plugin v1.0.5\r\nwritten by Hugo Flye from eMotion Tech \r\nIcons designed by Antony Soury\r\nwww.reprap-france.com");
        }
        /// <summary>
        /// Last round of plugin calls. All controls exist, so now you may modify them to your wishes.
        /// </summary>
        public void FinializeInitialize()
        {

        }
    }
}
