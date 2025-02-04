﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using FDK;

namespace TJAPlayer3
{
	internal class CActResultSongBar : CActivity
	{
		// コンストラクタ

		public CActResultSongBar()
		{
			base.b活性化してない = true;
		}


		// メソッド

		public void tアニメを完了させる()
		{
			this.ct登場用.n現在の値 = this.ct登場用.n終了値;
		}


		// CActivity 実装

		public override void On活性化()
		{
			// After performing calibration, inform the player that
			// calibration has been completed, rather than
			// displaying the song title as usual.


			var title = TJAPlayer3.IsPerformingCalibration
				? $"Calibration complete. InputAdjustTime is now {TJAPlayer3.ConfigIni.nInputAdjustTimeMs}ms"
				: TJAPlayer3.DTX[0].TITLE;

			using (var pfMusicName = new CFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.Result_MusicName_FontSize))
			{

				using (var bmpSongTitle = pfMusicName.DrawText(title, TJAPlayer3.Skin.Result_MusicName_ForeColor, TJAPlayer3.Skin.Result_MusicName_BackColor, TJAPlayer3.Skin.Font_Edge_Ratio))
				{
					this.txMusicName = TJAPlayer3.tCreateTexture(bmpSongTitle);
					txMusicName.vcScaling.X = TJAPlayer3.GetSongNameXScaling(ref txMusicName);
				}
			}

			using (var pfStageText = new CFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.Result_StageText_FontSize))
			{
				using (var bmpStageText = pfStageText.DrawText(TJAPlayer3.Skin.Game_StageText, TJAPlayer3.Skin.Result_StageText_ForeColor, TJAPlayer3.Skin.Result_StageText_BackColor, TJAPlayer3.Skin.Font_Edge_Ratio))
				{
					this.txStageText = TJAPlayer3.tCreateTexture(bmpStageText);
				}
			}

			base.On活性化();
		}
		public override void On非活性化()
		{
			if( this.ct登場用 != null )
			{
				this.ct登場用 = null;
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				TJAPlayer3.t安全にDisposeする( ref this.txMusicName );

				TJAPlayer3.t安全にDisposeする(ref this.txStageText);
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if( base.b活性化してない )
			{
				return 0;
			}
			if( base.b初めての進行描画 )
			{
				this.ct登場用 = new CCounter( 0, 270, 4, TJAPlayer3.Timer );
				base.b初めての進行描画 = false;
			}
			this.ct登場用.t進行();

			if (TJAPlayer3.ConfigIni.bEnableSkinV2)
			{
				if (TJAPlayer3.Skin.Result_v2_MusicName_ReferencePoint == CSkin.ReferencePoint.Center)
				{
					this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_v2_MusicName_X - ((this.txMusicName.szTextureSize.Width * txMusicName.vcScaling.X) / 2), TJAPlayer3.Skin.Result_v2_MusicName_Y);
				}
				else if (TJAPlayer3.Skin.Result_v2_MusicName_ReferencePoint == CSkin.ReferencePoint.Left)
				{
					this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_v2_MusicName_X, TJAPlayer3.Skin.Result_v2_MusicName_Y);
				}
				else
				{
					this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_v2_MusicName_X - this.txMusicName.szTextureSize.Width * txMusicName.vcScaling.X, TJAPlayer3.Skin.Result_v2_MusicName_Y);
				}
			}
			else
			{
				if (TJAPlayer3.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Center)
				{
					this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_MusicName_X - ((this.txMusicName.szTextureSize.Width * txMusicName.vcScaling.X) / 2), TJAPlayer3.Skin.Result_MusicName_Y);
				}
				else if (TJAPlayer3.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Left)
				{
					this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_MusicName_X, TJAPlayer3.Skin.Result_MusicName_Y);
				}
				else
				{
					this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_MusicName_X - this.txMusicName.szTextureSize.Width * txMusicName.vcScaling.X, TJAPlayer3.Skin.Result_MusicName_Y);
				}

				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
				{
					if (TJAPlayer3.Skin.Result_StageText_ReferencePoint == CSkin.ReferencePoint.Center)
					{
						this.txStageText.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_StageText_X - ((this.txStageText.szTextureSize.Width * txStageText.vcScaling.X) / 2), TJAPlayer3.Skin.Result_StageText_Y);
					}
					else if (TJAPlayer3.Skin.Result_StageText_ReferencePoint == CSkin.ReferencePoint.Right)
					{
						this.txStageText.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_StageText_X - this.txStageText.szTextureSize.Width, TJAPlayer3.Skin.Result_StageText_Y);
					}
					else
					{
						this.txStageText.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_StageText_X, TJAPlayer3.Skin.Result_StageText_Y);
					}
				}
			}


			if( !this.ct登場用.b終了値に達した )
			{
				return 0;
			}
			return 1;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter ct登場用;

		private CTexture txMusicName;

		private CTexture txStageText;
		//-----------------
		#endregion
	}
}
