﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using FDK;
using System.Reflection;
using DiscordRPC;

namespace TJAPlayer3
{
	internal class CStageTitle : CStage
	{
		// コンストラクタ

		public CStageTitle()
		{
			base.eStageID = CStage.EStage.Title;
			base.b活性化してない = true;
			base.list子Activities.Add(this.actFI = new CActFIFOBlack());
			base.list子Activities.Add(this.actFO = new CActFIFOBlack());
		}

		// CStage 実装

		public override void On活性化()
		{
			Trace.TraceInformation("タイトルステージを活性化します。");
			Trace.Indent();
			try
			{
				this.ct上移動用 = new CCounter();
				this.ct下移動用 = new CCounter();

				base.On活性化();

				TJAPlayer3.DiscordClient?.SetPresence(new RichPresence()
				{
					Details = "",
					State = "Title",
					Timestamps = new Timestamps(TJAPlayer3.StartupTime),
					Assets = new Assets()
					{
						LargeImageKey = TJAPlayer3.LargeImageKey,
						LargeImageText = TJAPlayer3.LargeImageText,
					}
				});
			}
			finally
			{
				Trace.TraceInformation("タイトルステージの活性化を完了しました。");
				Trace.Unindent();
			}
		}
		public override void On非活性化()
		{
			Trace.TraceInformation("タイトルステージを非活性化します。");
			Trace.Indent();
			try
			{
				this.ct上移動用 = null;
				this.ct下移動用 = null;
			}
			finally
			{
				Trace.TraceInformation("タイトルステージの非活性化を完了しました。");
				Trace.Unindent();
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			string[,] str = new string[,]{
				{ "演奏ゲーム","Taiko Mode"},
				{ "コンフィグ","Config"},
				{ "やめる","Quit"}
			};
			int lang = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja") ? 0 : 1;
			if ( !base.b活性化してない) {
				using (var pf = new CFontRenderer(TJAPlayer3.ConfigIni.FontName, 28))
				{
					texttexture[0] = this.文字テクスチャを生成する(str[0, lang], Color.White, Color.SaddleBrown, pf);
					texttexture[1] = this.文字テクスチャを生成する(str[1, lang], Color.White, Color.SaddleBrown, pf);
					texttexture[2] = this.文字テクスチャを生成する(str[2, lang], Color.White, Color.SaddleBrown, pf);
					texttexture[3] = this.文字テクスチャを生成する(str[0, lang], Color.White, Color.Black, pf);
					texttexture[4] = this.文字テクスチャを生成する(str[1, lang], Color.White, Color.Black, pf);
					texttexture[5] = this.文字テクスチャを生成する(str[2, lang], Color.White, Color.Black, pf);
				}
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				TJAPlayer3.t安全にDisposeする(ref texttexture);
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if (!base.b活性化してない)
			{
				#region [ 初めての進行描画 ]
				//---------------------
				if (base.b初めての進行描画)
				{
					if (TJAPlayer3.r直前のステージ == TJAPlayer3.stageStartUp)
					{
						this.actFI.tFadeIn開始();
						base.eフェーズID = CStage.Eフェーズ.タイトル_起動画面からのFadeIn;
					}
					else
					{
						this.actFI.tFadeIn開始();
						base.eフェーズID = CStage.Eフェーズ.共通_FadeIn;
					}
					base.b初めての進行描画 = false;
				}
				//---------------------
				#endregion

				// 進行

				#region [ カーソル上移動 ]
				//---------------------
				if (this.ct上移動用.b進行中)
				{
					this.ct上移動用.t進行();
					if (this.ct上移動用.b終了値に達した)
					{
						this.ct上移動用.t停止();
					}
				}
				//---------------------
				#endregion
				#region [ カーソル下移動 ]
				//---------------------
				if (this.ct下移動用.b進行中)
				{
					this.ct下移動用.t進行();
					if (this.ct下移動用.b終了値に達した)
					{
						this.ct下移動用.t停止();
					}
				}
				//---------------------
				#endregion

				// キー入力

				if (base.eフェーズID == CStage.Eフェーズ.共通_通常状態)        // 通常状態
				{
					if (TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.Escape))
						return (int)E戻り値.EXIT;

					if (TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.UpArrow) || TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.LeftArrow) || TJAPlayer3.Pad.bPressed(EPad.LBlue) || TJAPlayer3.Pad.bPressed(EPad.LBlue2P) && TJAPlayer3.ConfigIni.nPlayerCount >= 2)
						this.tカーソルを上へ移動する();

					if(TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.DownArrow) || TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.RightArrow) || TJAPlayer3.Pad.bPressed(EPad.RBlue) || TJAPlayer3.Pad.bPressed(EPad.RBlue2P) && TJAPlayer3.ConfigIni.nPlayerCount >= 2)
						this.tカーソルを下へ移動する();

					if (((TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.Return)) || TJAPlayer3.Pad.bPressed(EPad.LRed) || TJAPlayer3.Pad.bPressed(EPad.RRed) || (TJAPlayer3.Pad.bPressed(EPad.LRed2P) || TJAPlayer3.Pad.bPressed(EPad.RRed2P)) && TJAPlayer3.ConfigIni.nPlayerCount >= 2))
					{
						if ((this.n現在のカーソル行 == (int)E戻り値.GAMESTART - 1) && TJAPlayer3.Skin.soundゲーム開始音.b読み込み成功)
						{
							if (!((TJAPlayer3.InputManager.Keyboard.bIsKeyDown((int)SlimDXKeys.Key.LeftControl) || TJAPlayer3.InputManager.Keyboard.bIsKeyDown((int)SlimDXKeys.Key.RightControl)) && TJAPlayer3.InputManager.Keyboard.bIsKeyDown((int)SlimDXKeys.Key.A)))
								TJAPlayer3.Skin.soundゲーム開始音.t再生する();
						}
						else
						{
							TJAPlayer3.Skin.sound決定音.t再生する();
						}
						if (this.n現在のカーソル行 == (int)E戻り値.EXIT - 1)
						{
							return (int)E戻り値.EXIT;
						}
						this.actFO.tFadeOut開始();
						base.eフェーズID = CStage.Eフェーズ.共通_FadeOut;
					}
					//					if ( CDTXMania.InputManager.Keyboard.bIsKeyPressed( (int) Key.Space ) )
					//						Trace.TraceInformation( "DTXMania Title: SPACE key registered. " + CDTXMania.ct.nシステム時刻 );
				}

				// 描画

				if (TJAPlayer3.Tx.Title_Background != null)
					TJAPlayer3.Tx.Title_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);

				#region[ バージョン表示 ]
				//string strVersion = "KTT:J:A:I:2017072200";
				string strCreator = "https://github.com/Mr-Ojii/TJAPlayer3-f";
				AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
#if DEBUG
				TJAPlayer3.act文字コンソール.tPrint(4, 44, C文字コンソール.EFontType.白, "DEBUG BUILD");
#endif
				TJAPlayer3.act文字コンソール.tPrint(4, 4, C文字コンソール.EFontType.白, asmApp.Name + " Ver." + TJAPlayer3.VERSION + " (" + strCreator + ")");
				TJAPlayer3.act文字コンソール.tPrint(4, 24, C文字コンソール.EFontType.白, "Skin:" + TJAPlayer3.Skin.Name + " Ver." + TJAPlayer3.Skin.Version + " (" + TJAPlayer3.Skin.Creator + ")");
				//CDTXMania.act文字コンソール.tPrint(4, 24, C文字コンソール.EFontType.白, strSubTitle);
				TJAPlayer3.act文字コンソール.tPrint(4, (720 - 24), C文字コンソール.EFontType.白, "TJAPlayer3-f forked TJAPlayer3(AioiLight) forked TJAPlayer2 forPC(kairera0467)");
				#endregion


				if (TJAPlayer3.Tx.Title_InBar != null && TJAPlayer3.Tx.Title_AcBar != null)
				{
					for (int i = 0; i < 3; i++)
					{
						TJAPlayer3.Tx.Title_InBar.t2D描画(TJAPlayer3.app.Device, MENU_XT[i] - TJAPlayer3.Tx.Title_InBar.szTextureSize.Width / 2, MENU_YT);
					}

					if (this.ct下移動用.b進行中)
					{
						TJAPlayer3.Tx.Title_AcBar.vcScaling.X = this.ct下移動用.n現在の値 * 0.01f;
						TJAPlayer3.Tx.Title_AcBar.t2D描画(TJAPlayer3.app.Device, MENU_XT[this.n現在のカーソル行] - TJAPlayer3.Tx.Title_AcBar.szTextureSize.Width / 2 * this.ct下移動用.n現在の値 * 0.01f, MENU_YT);
					}
					else if (this.ct上移動用.b進行中)
					{
						TJAPlayer3.Tx.Title_AcBar.vcScaling.X = this.ct上移動用.n現在の値 * 0.01f;
						TJAPlayer3.Tx.Title_AcBar.t2D描画(TJAPlayer3.app.Device, MENU_XT[this.n現在のカーソル行] - TJAPlayer3.Tx.Title_AcBar.szTextureSize.Width / 2 * this.ct上移動用.n現在の値 * 0.01f, MENU_YT);
					}
					else
					{
						TJAPlayer3.Tx.Title_AcBar.vcScaling.X = 1.0f;
						TJAPlayer3.Tx.Title_AcBar.t2D描画(TJAPlayer3.app.Device, MENU_XT[this.n現在のカーソル行] - TJAPlayer3.Tx.Title_AcBar.szTextureSize.Width / 2, MENU_YT);
					}

					for (int i = 0; i < 3; i++)
					{
						if (i != this.n現在のカーソル行)
							texttexture[i].t2D描画(TJAPlayer3.app.Device, MENU_XT[i] - texttexture[i].szTextureSize.Width / 2, MENU_YT + 30);
						else
							texttexture[i + 3].t2D描画(TJAPlayer3.app.Device, MENU_XT[i] - texttexture[i + 3].szTextureSize.Width / 2, MENU_YT + 30);
					}
				}

				// URLの座標が押されたらブラウザで開いてやる 兼 マウスクリックのテスト
				// クライアント領域内のカーソル座標を取得する。
				// point.X、point.Yは負の値になることもある。
				SDL2.SDL.SDL_GetMouseState(out int x, out int y);
				var point = new Point(x, y);
				// クライアント領域の横幅を取得して、1280で割る。もちろんdouble型。
				var scaling = 1.000 * TJAPlayer3.app.ClientSize.Width / 1280;
				if (TJAPlayer3.InputManager.Mouse.bIsKeyPressed((int)SlimDXKeys.Mouse.Left))
				{
					if (point.X >= 180 * scaling && point.X <= 490 * scaling && point.Y >= 0 && point.Y <= 20 * scaling)
						CWebOpen.Open(strCreator);
				}


				CStage.Eフェーズ eフェーズid = base.eフェーズID;
				switch (eフェーズid)
				{
					case CStage.Eフェーズ.共通_FadeIn:
						if (this.actFI.On進行描画() != 0)
						{
							TJAPlayer3.Skin.soundタイトル音.t再生する();
							base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
						}
						break;

					case CStage.Eフェーズ.共通_FadeOut:
						if (this.actFO.On進行描画() == 0)
						{
							break;
						}
						base.eフェーズID = CStage.Eフェーズ.共通_終了状態;
						switch (this.n現在のカーソル行)
						{
							case (int)E戻り値.GAMESTART - 1:
								if (!((TJAPlayer3.InputManager.Keyboard.bIsKeyDown((int)SlimDXKeys.Key.LeftControl) || TJAPlayer3.InputManager.Keyboard.bIsKeyDown((int)SlimDXKeys.Key.RightControl)) && TJAPlayer3.InputManager.Keyboard.bIsKeyDown((int)SlimDXKeys.Key.A)))
									return (int)E戻り値.GAMESTART;
								else
									return (int)E戻り値.MAINTENANCE;

							case (int)E戻り値.CONFIG - 1:
								return (int)E戻り値.CONFIG;

							case (int)E戻り値.EXIT - 1:
								return (int)E戻り値.EXIT;
								//return ( this.n現在のカーソル行 + 1 );
						}
						break;

					case CStage.Eフェーズ.タイトル_起動画面からのFadeIn:
						if (this.actFI.On進行描画() != 0)
						{
							TJAPlayer3.Skin.soundタイトル音.t再生する();
							base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
						}
						break;
				}
			}
			return 0;
		}
		public enum E戻り値
		{
			継続 = 0,
			GAMESTART,
			//			OPTION,
			CONFIG,
			EXIT,
			MAINTENANCE
		}


		// その他

		#region [ private ]
		//-----------------
		private CTexture 文字テクスチャを生成する(string str文字, Color forecolor, Color backcolor, CFontRenderer pf) {
			using (var bmp = pf.DrawText_V(str文字, forecolor, backcolor, TJAPlayer3.Skin.Font_Edge_Ratio_Vertical)) {
				return TJAPlayer3.tCreateTexture(bmp);
			}
		}

		CTexture[] texttexture = new CTexture[6];
		private CActFIFOBlack actFI;
		private CActFIFOBlack actFO;
		private CCounter ct下移動用;
		private CCounter ct上移動用;
		//縦スタイル用
		private readonly int[] MENU_XT = {300,640,980 };
		private const int MENU_YT = 100;
		//------------------------------------
		private int n現在のカーソル行;
	
		private void tカーソルを下へ移動する()
		{
			if ( this.n現在のカーソル行 != (int) E戻り値.EXIT - 1 )
			{
				TJAPlayer3.Skin.soundカーソル移動音.t再生する();
				this.n現在のカーソル行++;
				this.ct下移動用.t開始( 0, 100, 1, TJAPlayer3.Timer );
				if( this.ct上移動用.b進行中 )
				{
					this.ct下移動用.n現在の値 = 100 - this.ct上移動用.n現在の値;
					this.ct上移動用.t停止();
				}
			}
		}
		private void tカーソルを上へ移動する()
		{
			if ( this.n現在のカーソル行 != (int) E戻り値.GAMESTART - 1 )
			{
				TJAPlayer3.Skin.soundカーソル移動音.t再生する();
				this.n現在のカーソル行--;
				this.ct上移動用.t開始( 0, 100, 1, TJAPlayer3.Timer );
				if( this.ct下移動用.b進行中 )
				{
					this.ct上移動用.n現在の値 = 100 - this.ct下移動用.n現在の値;
					this.ct下移動用.t停止();
				}
			}
		}
		//-----------------
		#endregion
	}
}
