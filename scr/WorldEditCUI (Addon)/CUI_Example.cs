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
using static WorldEdit.WorldEditCore;
using static XNA_WorldEditCUI;

namespace DNA.CastleMinerZ
{
	public partial class GameScreen : ScreenGroup
	{
		private void gameScreen_AfterDraw(object sender, DrawEventArgs e)
		{
			SpriteBatch spriteBatch = e.SpriteBatch;
			if (this._game.CurrentNetworkSession != null)
			{
				Matrix view = this.mainView.Camera.View;
				Matrix projection = this.mainView.Camera.GetProjection(e.Device);
				Matrix viewProj = viewMat * projMat;
				spriteBatch.Begin();
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
					NetworkGamer gamer = this._game.CurrentNetworkSession.AllGamers[i];
					if (gamer.Tag != null && !gamer.IsLocal)
					{
						Player player = (Player)gamer.Tag;
						if (player.Visible)
						{
							Vector3 worldPos = player.LocalPosition + new Vector3(0f, 2f, 0f);
							Vector4 spos = Vector4.Transform(worldPos, viewProj);
							if (spos.Z > 0f)
							{
								Vector3 screenPos = new Vector3(spos.X / spos.W, spos.Y / spos.W, spos.Z / spos.W);
								screenPos *= new Vector3(0.5f, -0.5f, 1f);
								screenPos += new Vector3(0.5f, 0.5f, 0f);
								screenPos *= new Vector3((float)Screen.Adjuster.ScreenRect.Width, (float)Screen.Adjuster.ScreenRect.Height, 1f);
								Vector2 textSize = this._game._nameTagFont.MeasureString(gamer.Gamertag);
								spriteBatch.DrawOutlinedText(this._game._nameTagFont, gamer.Gamertag, new Vector2(screenPos.X, screenPos.Y) - textSize / 2f, Color.White, Color.Black, 1);
							}
						}
					}
				}
				spriteBatch.End();
			}
		}
	}
}
