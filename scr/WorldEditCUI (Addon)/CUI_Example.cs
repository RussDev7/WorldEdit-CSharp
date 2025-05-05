using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using DNA.CastleMinerZ.Terrain;
using Microsoft.Xna.Framework;
using DNA.Net.GamerServices;
using DNA.CastleMinerZ.AI;
using DNA.CastleMinerZ.UI;
using DNA.Drawing.UI;
using DNA.Drawing;
using DNA.Timers;
using System;

// These namespaces are required for the 'WorldEditCUI' class.
using static XNA_WorldEditCUI;
using static WorldEdit;

namespace DNA.CastleMinerZ
{
    public partial class GameScreen : ScreenGroup
    {
        private void gameScreen_AfterDraw(object sender, DrawEventArgs e)
        {
            if (this.spriteBatch == null)
            {
                this.spriteBatch = new SpriteBatch(e.Device);
            }
            if (this._game.CurrentNetworkSession != null)
            {
                Matrix view = this.mainView.Camera.View;
                Matrix projection = this.mainView.Camera.GetProjection(e.Device);
                Matrix matrix = view * projection;
                this.spriteBatch.Begin();
                // this.spriteBatch.Begin();
                //
                /// <summary>
                /// This function is for 'WorldEditCUI' and it's purpose is to draw the visual selection outline box between two points within the 'WorldEdit' class.
                /// </summary>
                OutlineSelectionWithGrid(e.Device, view, projection, _pointToLocation1, _pointToLocation2);
                //
                // for (int i = 0; i < this._game.CurrentNetworkSession.AllGamers.Count; i++)
                for (int i = 0; i < this._game.CurrentNetworkSession.AllGamers.Count; i++)
                {
                    NetworkGamer networkGamer = this._game.CurrentNetworkSession.AllGamers[i];
                    if (networkGamer.Tag != null && !networkGamer.IsLocal)
                    {
                        Player player = (Player)networkGamer.Tag;
                        if (player.Visible)
                        {
                            Vector3 vector = player.LocalPosition + new Vector3(0f, 2f, 0f);
                            Vector4 vector2 = Vector4.Transform(vector, matrix);
                            if (vector2.Z > 0f)
                            {
                                Vector3 vector3 = new Vector3(vector2.X / vector2.W, vector2.Y / vector2.W, vector2.Z / vector2.W);
                                vector3 *= new Vector3(0.5f, -0.5f, 1f);
                                vector3 += new Vector3(0.5f, 0.5f, 0f);
                                vector3 *= new Vector3((float)Screen.Adjuster.ScreenRect.Width, (float)Screen.Adjuster.ScreenRect.Height, 1f);
                                Vector2 vector4 = this._game._nameTagFont.MeasureString(networkGamer.Gamertag);
                                this.spriteBatch.DrawOutlinedText(this._game._nameTagFont, networkGamer.Gamertag, new Vector2(vector3.X, vector3.Y) - vector4 / 2f, Color.White, Color.Black, 1);
                            }
                        }
                    }
                }
                this.spriteBatch.End();
            }
        }
    }
}