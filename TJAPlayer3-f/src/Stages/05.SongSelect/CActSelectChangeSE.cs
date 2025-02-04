﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using FDK;

namespace TJAPlayer3
{
	class CActSelectChangeSE : CActivity
	{
		public CActSelectChangeSE()
		{
			base.b活性化してない = true;
		}
		public override void On活性化()
		{
			if (this.b活性化してる)
				return;		

			base.On活性化();
		}
		public override void On非活性化()
		{
			if (this.b活性化してない)
				return;

			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if (!base.b活性化してない)
			{
				this.donglist = new CSound[2, TJAPlayer3.Skin.SECount];
				for (int nPlayer = 0; nPlayer < 2; nPlayer++)
				{
					for (int i = 0; i < TJAPlayer3.Skin.SECount; i++)
					{
						if (File.Exists(CSkin.Path(@"Sounds/Taiko/" + i.ToString() + @"/dong.ogg")))
							this.donglist[nPlayer, i] = TJAPlayer3.SoundManager.tCreateSound(CSkin.Path(@"Sounds/Taiko/" + i.ToString() + @"/dong.ogg"), ESoundGroup.SoundEffect);
						else
							this.donglist[nPlayer, i] = TJAPlayer3.SoundManager.tCreateSound(CSkin.Path(@"Sounds/Taiko/dong.ogg"), ESoundGroup.SoundEffect);

						if (TJAPlayer3.ConfigIni.nPlayerCount >= 2 && TJAPlayer3.ConfigIni.b2P演奏時のSEの左右 && donglist[nPlayer, i] != null)
						{
							this.donglist[nPlayer, i].nPanning = (nPlayer * 200) - 100;
						}
					}
				}

				this.SEName = new CTexture[2];
				this.NameMoving = new CTexture[2];
				this.SENameList = new CTexture[TJAPlayer3.Skin.SECount];

				using (var font = new CFontRenderer(TJAPlayer3.ConfigIni.FontName, 30))
					for (int i = 0; i < TJAPlayer3.Skin.SECount; i++)
					{
						using (var bmp = font.DrawText(TJAPlayer3.Skin.SENames[i], Color.White, Color.Black, TJAPlayer3.Skin.Font_Edge_Ratio))
							this.SENameList[i] = TJAPlayer3.tCreateTexture(bmp);
					}

				this.SENameChanger(0);
				this.SENameChanger(1);
			}

			base.OnManagedリソースの作成();
        }
        public override void OnManagedリソースの解放()
		{
			if (!base.b活性化してない)
			{
				for (int nPlayer = 0; nPlayer < 2; nPlayer++)
				{
					for (int i = 0; i < TJAPlayer3.Skin.SECount; i++)
					{
						this.donglist[nPlayer, i].t解放する();
					}
				}

				//Classは参照渡しであるため、ListをDisposeするだけでよい
				TJAPlayer3.t安全にDisposeする(ref this.SENameList);
			}
			base.OnManagedリソースの解放();
        }

        public override int On進行描画()
		{
			if (this.b活性化してない)
				return 0;

			#region [ 初めての進行描画 ]
			//-----------------
			if (this.b初めての進行描画)
			{
				base.b初めての進行描画 = false;
			}
			//-----------------
			#endregion

			for (int nPlayer = 0; nPlayer < 2; nPlayer++)
			{
				this.ct登場退場アニメ用[nPlayer].t進行();
				if (this.ePhase[nPlayer] == EChangeSEPhase.Active)
				{
					this.boxたちの描画(TJAPlayer3.Skin.ChangeSE_Box_X[nPlayer], TJAPlayer3.Skin.ChangeSE_Box_Y[nPlayer], nPlayer);	
				}
				else if (this.ePhase[nPlayer] == EChangeSEPhase.AnimationIn)
				{
					int y = (int)((TJAPlayer3.Skin.ChangeSE_Box_Y[nPlayer]) + (float)((Math.Sin(this.ct登場退場アニメ用[nPlayer].n現在の値 / 100.0) - 0.9) * -500f));
					this.boxたちの描画(TJAPlayer3.Skin.ChangeSE_Box_X[nPlayer], y, nPlayer);

					if (this.ct登場退場アニメ用[nPlayer].b終了値に達した)
						this.ePhase[nPlayer] = EChangeSEPhase.Active;
				}
				else if (this.ePhase[nPlayer] == EChangeSEPhase.AnimationOut)
				{
					int y = (int)((TJAPlayer3.Skin.ChangeSE_Box_Y[nPlayer]) + (float)((Math.Sin((this.ct登場退場アニメ用[nPlayer].n終了値 - this.ct登場退場アニメ用[nPlayer].n現在の値) / 100.0) - 0.9) * -500f));
					this.boxたちの描画(TJAPlayer3.Skin.ChangeSE_Box_X[nPlayer], y, nPlayer);

					if (this.ct登場退場アニメ用[nPlayer].b終了値に達した)
						this.ePhase[nPlayer] = EChangeSEPhase.Inactive;
				}
			}

			if (this.ePhase[0] == EChangeSEPhase.Active) {
				if (TJAPlayer3.Pad.bPressed(EPad.LRed) || TJAPlayer3.Pad.bPressed(EPad.RRed) || TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.Return))
				{
					TJAPlayer3.Skin.sound決定音?.t再生する();
					this.tDeativateChangeSE(0);
				}
				if ((TJAPlayer3.Pad.bPressed(EPad.LBlue) || TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.LeftArrow)) && TJAPlayer3.Skin.SECount != 0)
				{
					TJAPlayer3.Skin.NowSENum[0]--;
					if (TJAPlayer3.Skin.NowSENum[0] < 0)
						TJAPlayer3.Skin.NowSENum[0] = TJAPlayer3.Skin.SECount - 1;
					this.donglist[0, TJAPlayer3.Skin.NowSENum[0]]?.t再生を開始する();
					this.MoveStart(EMoving.LeftMoving, 0);
					this.SENameChanger(0);
				}
				if ((TJAPlayer3.Pad.bPressed(EPad.RBlue) || TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.RightArrow)) && TJAPlayer3.Skin.SECount != 0)
				{
					TJAPlayer3.Skin.NowSENum[0]++;
					if (TJAPlayer3.Skin.NowSENum[0] > TJAPlayer3.Skin.SECount - 1)
						TJAPlayer3.Skin.NowSENum[0] = 0;
					this.donglist[0, TJAPlayer3.Skin.NowSENum[0]]?.t再生を開始する();
					this.MoveStart(EMoving.RightMoving, 0);
					this.SENameChanger(0);
				}
			}
			if (this.ePhase[1] == EChangeSEPhase.Active)
			{
				if (TJAPlayer3.Pad.bPressed(EPad.LRed2P) || TJAPlayer3.Pad.bPressed(EPad.RRed2P) || (TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.Return) && TJAPlayer3.stage選曲.actDifficultySelect.選択済み[0]))
				{
					TJAPlayer3.Skin.sound決定音?.t再生する();
					this.tDeativateChangeSE(1);
				}
				if ((TJAPlayer3.Pad.bPressed(EPad.LBlue2P) || (TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.LeftArrow) && TJAPlayer3.stage選曲.actDifficultySelect.選択済み[0])) && TJAPlayer3.Skin.SECount != 0)
				{
					TJAPlayer3.Skin.NowSENum[1]--;
					if (TJAPlayer3.Skin.NowSENum[1] < 0)
						TJAPlayer3.Skin.NowSENum[1] = TJAPlayer3.Skin.SECount - 1;
					this.donglist[1, TJAPlayer3.Skin.NowSENum[1]]?.t再生を開始する();
					this.MoveStart(EMoving.LeftMoving, 1);
					this.SENameChanger(1);
				}
				if ((TJAPlayer3.Pad.bPressed(EPad.RBlue2P) || (TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.RightArrow) && TJAPlayer3.stage選曲.actDifficultySelect.選択済み[0])) && TJAPlayer3.Skin.SECount != 0)
				{
					TJAPlayer3.Skin.NowSENum[1]++;
					if (TJAPlayer3.Skin.NowSENum[1] > TJAPlayer3.Skin.SECount - 1)
						TJAPlayer3.Skin.NowSENum[1] = 0;
					this.donglist[1, TJAPlayer3.Skin.NowSENum[1]]?.t再生を開始する();
					this.MoveStart(EMoving.RightMoving, 1);
					this.SENameChanger(1);
				}
			}

			return base.On進行描画();
		}

		private void MoveStart(EMoving lr,int nPlayer) {
			this.eMoving[nPlayer] = lr;
			this.ct変更アニメ用[nPlayer].t時間Reset();
			this.ct変更アニメ用[nPlayer].n現在の値 = 0;
			
		}

		private void boxたちの描画(int x, int y, int nPlayer)
		{
			if (TJAPlayer3.Tx.ChangeSE_Box != null)
				TJAPlayer3.Tx.ChangeSE_Box.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x, y);

			if (TJAPlayer3.Tx.ChangeSE_Num != null)
			{
				for (int i = 0; i < TJAPlayer3.Skin.SECount.ToString().Length; i++)
				{
					var number = (int)(TJAPlayer3.Skin.SECount / Math.Pow(10, TJAPlayer3.Skin.SECount.ToString().Length - i - 1) % 10);
					Rectangle rectangle = new Rectangle(TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Width / 10 * number, 0, TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Width / 10, TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Height);
					TJAPlayer3.Tx.ChangeSE_Num.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x + (i * TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Width / 10) + 20, y - 260, rectangle);
				}
				for (int i = 0; i < (TJAPlayer3.Skin.NowSENum[nPlayer] + 1).ToString().Length; i++)
				{
					var number = (int)((TJAPlayer3.Skin.NowSENum[nPlayer] + 1) / Math.Pow(10, i) % 10);
					Rectangle rectangle = new Rectangle(TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Width / 10 * number, 0, TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Width / 10, TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Height);
					TJAPlayer3.Tx.ChangeSE_Num.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x - (i * TJAPlayer3.Tx.ChangeSE_Num.szTextureSize.Width / 10) - 20, y - 260, rectangle);
				}
			}
			if (eMoving[nPlayer] == EMoving.None)
			{
				if (TJAPlayer3.Tx.ChangeSE_Note != null)
				{
					TJAPlayer3.Tx.ChangeSE_Note.Opacity = 0xff;
					TJAPlayer3.Tx.ChangeSE_Note.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x, y);
				}
				this.SEName[nPlayer].Opacity = 0xff;
				this.SEName[nPlayer]?.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x, y - 50);
			}
			else if (eMoving[nPlayer] == EMoving.LeftMoving)
			{
				this.ct変更アニメ用[nPlayer].t進行();
				if (TJAPlayer3.Tx.ChangeSE_Note != null)
				{
					TJAPlayer3.Tx.ChangeSE_Note.Opacity = ct変更アニメ用[nPlayer].n現在の値;
					TJAPlayer3.Tx.ChangeSE_Note.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x + (int)((ct変更アニメ用[nPlayer].n現在の値 - ct変更アニメ用[nPlayer].n終了値) * 0.5f), y);
					TJAPlayer3.Tx.ChangeSE_Note.Opacity = ct変更アニメ用[nPlayer].n終了値 - ct変更アニメ用[nPlayer].n現在の値;
					TJAPlayer3.Tx.ChangeSE_Note.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x + (int)((ct変更アニメ用[nPlayer].n現在の値) * 0.5f), y);
				}
				this.NameMoving[nPlayer].Opacity = ct変更アニメ用[nPlayer].n終了値 - ct変更アニメ用[nPlayer].n現在の値;
				this.NameMoving[nPlayer]?.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x + (int)((ct変更アニメ用[nPlayer].n現在の値) * 0.5f), y - 50);
				this.SEName[nPlayer].Opacity = ct変更アニメ用[nPlayer].n現在の値;
				this.SEName[nPlayer]?.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x + (int)((ct変更アニメ用[nPlayer].n現在の値 - ct変更アニメ用[nPlayer].n終了値) * 0.5f), y - 50);
				if (this.ct変更アニメ用[nPlayer].b終了値に達した)
					this.eMoving[nPlayer] = EMoving.None;
			}
			else
			{
				this.ct変更アニメ用[nPlayer].t進行();
				if (TJAPlayer3.Tx.ChangeSE_Note != null)
				{
					TJAPlayer3.Tx.ChangeSE_Note.Opacity = ct変更アニメ用[nPlayer].n現在の値;
					TJAPlayer3.Tx.ChangeSE_Note.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x - (int)((ct変更アニメ用[nPlayer].n現在の値 - ct変更アニメ用[nPlayer].n終了値) * 0.5f), y);
					TJAPlayer3.Tx.ChangeSE_Note.Opacity = ct変更アニメ用[nPlayer].n終了値 - ct変更アニメ用[nPlayer].n現在の値;
					TJAPlayer3.Tx.ChangeSE_Note.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x - (int)((ct変更アニメ用[nPlayer].n現在の値) * 0.5f), y);
				}

				this.NameMoving[nPlayer].Opacity = ct変更アニメ用[nPlayer].n終了値 - ct変更アニメ用[nPlayer].n現在の値;
				this.NameMoving[nPlayer]?.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x - (int)((ct変更アニメ用[nPlayer].n現在の値) * 0.5f), y - 50);
				this.SEName[nPlayer].Opacity = ct変更アニメ用[nPlayer].n現在の値;
				this.SEName[nPlayer]?.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, x - (int)((ct変更アニメ用[nPlayer].n現在の値 - ct変更アニメ用[nPlayer].n終了値) * 0.5f), y - 50);
				if (this.ct変更アニメ用[nPlayer].b終了値に達した)
					this.eMoving[nPlayer] = EMoving.None;
			}
		}

		private void SENameChanger(int nPlayer)
		{
			if (TJAPlayer3.Skin.SECount != 0)
			{
				this.NameMoving[nPlayer] = this.SEName[nPlayer];
				this.SEName[nPlayer] = this.SENameList[TJAPlayer3.Skin.NowSENum[nPlayer]];
			}
		}

		/// <summary>
		/// 音色切り替えをアクティブ化
		/// </summary>
		/// <param name="nPlayer">プレイヤー番号</param>
		public void tActivateChangeSE(int nPlayer)
		{
			if (TJAPlayer3.Skin.SECount != 0 && ePhase[nPlayer] == EChangeSEPhase.Inactive)
			{
				ePhase[nPlayer] = EChangeSEPhase.AnimationIn;
				ct登場退場アニメ用[nPlayer].t時間Reset();
				ct登場退場アニメ用[nPlayer].n現在の値 = 0;
			}
		}

		/// <summary>
		/// 音色切り替えを非アクティブ化
		/// </summary>
		/// <param name="nPlayer">プレイヤー番号</param>
		public void tDeativateChangeSE(int nPlayer)
		{
			if (ePhase[nPlayer] == EChangeSEPhase.Active) {
				ePhase[nPlayer] = EChangeSEPhase.AnimationOut;
				ct登場退場アニメ用[nPlayer].t時間Reset();
				ct登場退場アニメ用[nPlayer].n現在の値 = 0; 
			}
		}


		#region[private]
		private EChangeSEPhase[] ePhase = { EChangeSEPhase.Inactive, EChangeSEPhase.Inactive };

		private EMoving[] eMoving = { EMoving.None, EMoving.None };

		public bool[] bIsActive
		{
			get
			{
				return new bool[] { (ePhase[0] != EChangeSEPhase.Inactive), (ePhase[1] != EChangeSEPhase.Inactive) };
			}
		}

		private CCounter[] ct登場退場アニメ用 = { new CCounter(0, 203, 2, TJAPlayer3.Timer), new CCounter(0, 203, 2, TJAPlayer3.Timer) };
		private CCounter[] ct変更アニメ用 = { new CCounter(0, 255, 1, TJAPlayer3.Timer), new CCounter(0, 255, 1, TJAPlayer3.Timer) };

		private enum EMoving
		{
			None,
			LeftMoving,
			RightMoving
		}

		private enum EChangeSEPhase
		{
			Inactive,
			AnimationIn,
			Active,
			AnimationOut
		}

		private CSound[,] donglist;
		private CTexture[] SEName;
		private CTexture[] NameMoving;
		private CTexture[] SENameList;
        #endregion
    }
}
