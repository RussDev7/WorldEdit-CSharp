/* 
Copyright (c) 2025 RussDev7

This source is subject to the GNU General Public License v3.0 (GPLv3).
See https://www.gnu.org/licenses/gpl-3.0.html.

THIS PROGRAM IS FREE SOFTWARE: YOU CAN REDISTRIBUTE IT AND/OR MODIFY 
IT UNDER THE TERMS OF THE GNU GENERAL PUBLIC LICENSE AS PUBLISHED BY 
THE FREE SOFTWARE FOUNDATION, EITHER VERSION 3 OF THE LICENSE, OR 
(AT YOUR OPTION) ANY LATER VERSION.

THIS PROGRAM IS DISTRIBUTED IN THE HOPE THAT IT WILL BE USEFUL, 
BUT WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF 
MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE. SEE THE 
GNU GENERAL PUBLIC LICENSE FOR MORE DETAILS.
*/

using DNA.CastleMinerZ.Utils.Threading;
using Microsoft.Xna.Framework.Graphics;
using DNA.CastleMinerZ.Achievements;
using Microsoft.Xna.Framework.Audio;
using DNA.CastleMinerZ.Inventory;
using DNA.CastleMinerZ.Terrain;
using Microsoft.Xna.Framework;
using DNA.Net.GamerServices;
using DNA.CastleMinerZ.UI;
using DNA.Distribution;
using System.Threading;
using DNA.Drawing.UI;
using DNA.IO.Storage;
using DNA.Drawing;
using DNA.Timers;
using WorldEdit; // Required namespace for the 'WorldEdit' class.
using DNA.Net;
using System;

namespace DNA.CastleMinerZ
{
    public partial class CastleMinerZGame : DNAGame
    {
        protected override void Update(GameTime gameTime)
        {
            /// {
            ///
            /// <summary>
            /// Pumps WorldEdit async queues each frame so queued edits and yields continue running.
            /// </summary>
            AsyncBlockPlacer.Pump();
            AsyncFrameYield.Pump();
            ///
            /// <summary>
            /// Ensure the nav-wand timer is running.
            /// Safe to call every frame because StartNavWandTimer() ignores duplicate starts.
            /// </summary>
            WorldEditRuntime.StartNavWandTimer();
            ///
            /// if (this.CurrentWorld != null)
            if (this.CurrentWorld != null)
            {
                this.CurrentWorld.Update(gameTime);
            }
            this.UpdateMusic(gameTime);
            if (this._terrain != null)
            {
                this._terrain.GlobalUpdate(gameTime);
                if (this._terrain.MinimallyLoaded && this._waitForTerrainCallback != null)
                {
                    this._waitForTerrainCallback();
                    this._waitForTerrainCallback = null;
                }
            }
            if (this.PlayerStats != null)
            {
                this.PlayerStats.TimeInFull += gameTime.ElapsedGameTime;
                this.PlayerStats.TimeOfPurchase = DateTime.UtcNow;
                if (this.FrontEnd != null && this.mainScreenGroup.CurrentScreen == this.FrontEnd)
                {
                    this.PlayerStats.TimeInMenu += gameTime.ElapsedGameTime;
                }
                if (base.CurrentNetworkSession != null && base.CurrentNetworkSession.SessionType == NetworkSessionType.PlayerMatch)
                {
                    this.PlayerStats.TimeOnline += gameTime.ElapsedGameTime;
                }
            }
            if (this.RequestEndGame)
            {
                this.RequestEndGame = false;
                this.EndGame(true);
            }
            TaskDispatcher.Instance.RunMainThreadTasks();
            base.Update(gameTime);
        }
    }
}