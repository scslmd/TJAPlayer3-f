using FDK;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
	internal class CActSelect曲リスト : CActivity
	{
		// プロパティ

		public bool bIsEnumeratingSongs
		{
			get;
			set;
		}
		public bool bスクロール中
		{
			get
			{
				if (this.n目標のスクロールカウンタ == 0)
				{
					return (this.n現在のスクロールカウンタ != 0);
				}
				return true;
			}
		}
		public int[] n現在のアンカ難易度レベル
		{
			get;
			private set;
		}
		public int[] n現在選択中の曲の難易度レベル
		{
			get
			{
				return new int[] { this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r現在選択中の曲, 0), this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r現在選択中の曲 , 1) };
			}
		}
		public Cスコア r現在選択中のスコア
		{
			get
			{
				if (this.r現在選択中の曲 != null)
					return this.r現在選択中の曲.arスコア;
				return null;
			}
		}

		public C曲リストノード r現在選択中の曲
		{
			get;
			private set;
		}

		// t選択曲が変更された()内で使う、直前の選曲の保持
		// (前と同じ曲なら選択曲変更に掛かる再計算を省略して高速化するため)
		private C曲リストノード song_last = null;

		// コンストラクタ

		public CActSelect曲リスト()
		{
			this.r現在選択中の曲 = null;
			n現在のアンカ難易度レベル = new int[2];
			for (int nPlayer = 0; nPlayer < 2; nPlayer++)
				this.n現在のアンカ難易度レベル[nPlayer] = TJAPlayer3.ConfigIni.nDefaultCourse;
			base.b活性化してない = true;
			this.bIsEnumeratingSongs = false;
		}


		// メソッド

		public int n現在のアンカ難易度レベルに最も近い難易度レベルを返す(C曲リストノード song, int nPlayer)
		{
			// 事前チェック。

			if (song == null)
				return this.n現在のアンカ難易度レベル[nPlayer];  // 曲がまったくないよ

			if (song.arスコア.譜面情報.b譜面が存在する[this.n現在のアンカ難易度レベル[nPlayer]] != false)
				return this.n現在のアンカ難易度レベル[nPlayer];  // 難易度ぴったりの曲があったよ

			if ((song.eNodeType == C曲リストノード.ENodeType.BOX) || (song.eNodeType == C曲リストノード.ENodeType.BACKBOX))
				return 0;                               // BOX と BACKBOX は関係無いよ


			// 現在のアンカレベルから、難易度上向きに検索開始。

			int n最も近いレベル = this.n現在のアンカ難易度レベル[nPlayer];

			for (int i = 0; i < (int)Difficulty.Total; i++)
			{
				if (song.arスコア.譜面情報.b譜面が存在する[n最も近いレベル] != false)
					break;  // 曲があった。

				n最も近いレベル = (n最も近いレベル + 1) % (int)Difficulty.Total;  // 曲がなかったので次の難易度レベルへGo。（5以上になったら0に戻る。）
			}


			// 見つかった曲がアンカより下のレベルだった場合……
			// アンカから下向きに検索すれば、もっとアンカに近い曲があるんじゃね？

			if (n最も近いレベル < this.n現在のアンカ難易度レベル[nPlayer])
			{
				// 現在のアンカレベルから、難易度下向きに検索開始。

				n最も近いレベル = this.n現在のアンカ難易度レベル[nPlayer];

				for (int i = 0; i < (int)Difficulty.Total; i++)
				{
					if (song.arスコア.譜面情報.b譜面が存在する[n最も近いレベル] != false)
						break;  // 曲があった。

					n最も近いレベル = ((n最も近いレベル - 1) + (int)Difficulty.Total) % (int)Difficulty.Total;    // 曲がなかったので次の難易度レベルへGo。（0未満になったら4に戻る。）
				}
			}

			return n最も近いレベル;
		}
		private List<C曲リストノード> GetSongListWithinMe(C曲リストノード song)
		{
			if (song.r親ノード == null)                 // root階層のノートだったら
			{
				return TJAPlayer3.SongsManager.list曲ルート; // rootのリストを返す
			}
			else
			{
				if ((song.r親ノード.list子リスト != null) && (song.r親ノード.list子リスト.Count > 0))
				{
					return song.r親ノード.list子リスト;
				}
				else
				{
					return null;
				}
			}
		}


		public delegate void DGSortFunc(List<C曲リストノード> songList, int order, params object[] p);
		/// <summary>
		/// 主にCSong管理.cs内にあるソート機能を、delegateで呼び出す。
		/// </summary>
		/// <param name="sf">ソート用に呼び出すメソッド</param>
		/// <param name="order">-1=降順, 1=昇順</param>
		public void t曲リストのソート(DGSortFunc sf, int order, params object[] p)
		{
			List<C曲リストノード> songList = GetSongListWithinMe(this.r現在選択中の曲);
			if (songList == null)
			{
				// 何もしない;
			}
			else
			{
				//				CDTXMania.SongsManager.t曲リストのソート3_演奏回数の多い順( songList, eInst, order );
				sf(songList, order, p);
				//				this.r現在選択中の曲 = CDTXMania
				this.t現在選択中の曲を元に曲バーを再構成する();
			}
		}

		public void RandomSelect(C曲リストノード c曲) {
			this.r現在選択中の曲 = c曲;
			this.t現在選択中の曲を元に曲バーを再構成する();

			this.t選択曲が変更された(false);                                 // #27648 項目数変更を反映させる
            this.b選択曲が変更された = true;

			TJAPlayer3.stage選曲.t選択曲変更通知();
		}

		public void tBOXに入る()
		{
			if (TJAPlayer3.ConfigIni.OpenOneSide) {
				List<C曲リストノード> list = TJAPlayer3.SongsManager.list曲ルート;
				list.InsertRange(list.IndexOf(this.r現在選択中の曲) + 1, this.r現在選択中の曲.list子リスト);
				int n回数 = this.r現在選択中の曲.Openindex;
				for (int index = 0; index <= n回数; index++)
					this.r現在選択中の曲 = this.r次の曲(r現在選択中の曲);
				list.RemoveAt(list.IndexOf(this.r現在選択中の曲.r親ノード));
				this.t現在選択中の曲を元に曲バーを再構成する();
				this.t選択曲が変更された(false);                                 // #27648 項目数変更を反映させる
				this.b選択曲が変更された = true;
			}
			else
			{

				if ((this.r現在選択中の曲.list子リスト != null) && (this.r現在選択中の曲.list子リスト.Count > 0))
				{
					if (this.r現在選択中の曲.list子リスト.Count > 1 && this.r現在選択中の曲.Openindex < this.r現在選択中の曲.list子リスト.Count)//見た目だけだと気づきにくいが、Indexがずれてるので、[1]にする。閉じるだけのものは、[0]にする。
						this.r現在選択中の曲 = this.r現在選択中の曲.list子リスト[this.r現在選択中の曲.Openindex];
					else
						this.r現在選択中の曲 = this.r現在選択中の曲.list子リスト[0];
					this.t現在選択中の曲を元に曲バーを再構成する();
					this.t選択曲が変更された(false);                                 // #27648 項目数変更を反映させる
					this.b選択曲が変更された = true;
				}
			}
			TJAPlayer3.stage選曲.t選択曲変更通知();
		}
		public void tBOXを出る()
		{
			if (TJAPlayer3.ConfigIni.OpenOneSide) {
				List<C曲リストノード> list = TJAPlayer3.SongsManager.list曲ルート;
				this.r現在選択中の曲.r親ノード.Openindex = r現在選択中の曲.r親ノード.list子リスト.IndexOf(this.r現在選択中の曲);
				list.Insert(list.IndexOf(this.r現在選択中の曲) + 1, this.r現在選択中の曲.r親ノード);
				this.r現在選択中の曲 = this.r次の曲(r現在選択中の曲);
				for (int index = 0; index < list.Count; index++) {
					if (this.r現在選択中の曲.list子リスト.Contains(list[index]))
					{
						list.RemoveAt(index); 
						index--;
					}

				}
				this.t現在選択中の曲を元に曲バーを再構成する();
				this.t選択曲が変更された(false);                                 // #27648 項目数変更を反映させる
				this.b選択曲が変更された = true;
			}
			else
			{
				if (this.r現在選択中の曲.r親ノード != null)
				{
					List<C曲リストノード> list = r現在選択中の曲.r親ノード.list子リスト;//t前の曲 t次の曲から、流用。
					this.r現在選択中の曲.r親ノード.Openindex = r現在選択中の曲.r親ノード.list子リスト.IndexOf(this.r現在選択中の曲);
					this.r現在選択中の曲 = this.r現在選択中の曲.r親ノード;
					this.t現在選択中の曲を元に曲バーを再構成する();
					this.t選択曲が変更された(false);                                 // #27648 項目数変更を反映させる
					this.b選択曲が変更された = true;
				}
			}
			TJAPlayer3.stage選曲.t選択曲変更通知();
		}
		public void t次に移動()
		{
			if (this.r現在選択中の曲 != null)
			{
				this.n目標のスクロールカウンタ += 100;
			}
			this.b選択曲が変更された = true;
		}
		public void t前に移動()
		{
			if (this.r現在選択中の曲 != null)
			{
				this.n目標のスクロールカウンタ -= 100;
			}
			this.b選択曲が変更された = true;
		}
		public void tかなり次に移動()
		{
			if (this.r現在選択中の曲 != null)
			{
				for (int i = 0; i < TJAPlayer3.ConfigIni.SongSelectSkipCount; i++)
					this.r現在選択中の曲 = r次の曲(r現在選択中の曲);
			}
			this.t現在選択中の曲を元に曲バーを再構成する();
			this.t選択曲が変更された(false);
			this.b選択曲が変更された = true;
			TJAPlayer3.stage選曲.t選択曲変更通知();
		}
		public void tかなり前に移動()
		{
			if (this.r現在選択中の曲 != null)
			{
				for (int i = 0; i < TJAPlayer3.ConfigIni.SongSelectSkipCount; i++)
					this.r現在選択中の曲 = r前の曲(r現在選択中の曲);
			}
			this.t現在選択中の曲を元に曲バーを再構成する();
			this.t選択曲が変更された(false);
			this.b選択曲が変更された = true;
			TJAPlayer3.stage選曲.t選択曲変更通知();
		}
		public void tフォルダのはじめに移動()
		{
			if (this.r現在選択中の曲 != null)
			{
				if (this.r現在選択中の曲.r親ノード != null)
					this.r現在選択中の曲 = this.r現在選択中の曲.r親ノード.list子リスト[0];
				else
					this.r現在選択中の曲 = TJAPlayer3.SongsManager.list曲ルート[0];
			}
			this.t現在選択中の曲を元に曲バーを再構成する();
			this.t選択曲が変更された(false);
			this.b選択曲が変更された = true;
			TJAPlayer3.stage選曲.t選択曲変更通知();
		}
		public void tフォルダの最後に移動()
		{
			if (this.r現在選択中の曲 != null)
			{
				if (this.r現在選択中の曲.r親ノード != null)
					this.r現在選択中の曲 = this.r現在選択中の曲.r親ノード.list子リスト[this.r現在選択中の曲.r親ノード.list子リスト.Count - 1];
				else
					this.r現在選択中の曲 = TJAPlayer3.SongsManager.list曲ルート[TJAPlayer3.SongsManager.list曲ルート.Count - 1];
			}
			this.t現在選択中の曲を元に曲バーを再構成する();
			this.t選択曲が変更された(false);
			this.b選択曲が変更された = true;
			TJAPlayer3.stage選曲.t選択曲変更通知();
		}
		public void t難易度レベルをひとつ進める(int nPlayer)
		{
			if ((this.r現在選択中の曲 == null) || (this.r現在選択中の曲.nスコア数 <= 1))
				return;     // 曲にスコアが０～１個しかないなら進める意味なし。


			// 難易度レベルを＋１し、現在選曲中のスコアを変更する。

			this.n現在のアンカ難易度レベル[nPlayer] = this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r現在選択中の曲, nPlayer);

			for (int i = 0; i < (int)Difficulty.Total; i++)
			{
				this.n現在のアンカ難易度レベル[nPlayer] = (this.n現在のアンカ難易度レベル[nPlayer] + 1) % (int)Difficulty.Total;  // ５以上になったら０に戻る。
				if (this.r現在選択中の曲.arスコア.譜面情報.b譜面が存在する[this.n現在のアンカ難易度レベル[nPlayer]] != false)    // 曲が存在してるならここで終了。存在してないなら次のレベルへGo。
					break;
			}
		}
		/// <summary>
		/// 不便だったから作った。
		/// </summary>
		public void t難易度レベルをひとつ戻す(int nPlayer)
		{
			if ((this.r現在選択中の曲 == null) || (this.r現在選択中の曲.nスコア数 <= 1))
				return;     // 曲にスコアが０～１個しかないなら進める意味なし。


			// 難易度レベルを＋１し、現在選曲中のスコアを変更する。

			this.n現在のアンカ難易度レベル[nPlayer] = this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r現在選択中の曲, nPlayer);

			this.n現在のアンカ難易度レベル[nPlayer]--;
			if (this.n現在のアンカ難易度レベル[nPlayer] < 0) // 0より下になったら4に戻す。
			{
				this.n現在のアンカ難易度レベル[nPlayer] = 4;
			}

			//2016.08.13 kairera0467 かんたん譜面が無い譜面(ふつう、むずかしいのみ)で、難易度を最上位に戻せない不具合の修正。
			bool bLabel0NotFound = true;
			for (int i = this.n現在のアンカ難易度レベル[nPlayer]; i >= 0; i--)
			{
				if (this.r現在選択中の曲.arスコア.譜面情報.b譜面が存在する[i] != false)
				{
					this.n現在のアンカ難易度レベル[nPlayer] = i;
					bLabel0NotFound = false;
					break;
				}
			}
			if (bLabel0NotFound)
			{
				for (int i = 4; i >= 0; i--)
				{
					if (this.r現在選択中の曲.arスコア.譜面情報.b譜面が存在する[i] != false)
					{
						this.n現在のアンカ難易度レベル[nPlayer] = i;
						break;
					}
				}
			}
		}


		/// <summary>
		/// 曲リストをリセットする
		/// </summary>
		/// <param name="cs"></param>
		public void Refresh(CSongsManager cs, bool bRemakeSongTitleBar)      // #26070 2012.2.28 yyagi
		{
			//			this.On非活性化();

			if (cs != null && cs.list曲ルート.Count > 0)    // 新しい曲リストを検索して、1曲以上あった
			{
				TJAPlayer3.SongsManager = cs;
				if (this.r現在選択中の曲 != null)          // r現在選択中の曲==null とは、「最初songlist.dbが無かった or 検索したが1曲もない」
				{
					this.r現在選択中の曲 = searchCurrentBreadcrumbsPosition(TJAPlayer3.SongsManager.list曲ルート, this.r現在選択中の曲.strBreadcrumbs);
					if (bRemakeSongTitleBar)                    // 選曲画面以外に居るときには再構成しない (非活性化しているときに実行すると例外となる)
					{
						this.t現在選択中の曲を元に曲バーを再構成する();
					}
					return;
				}
				
			}
			if (this.b活性化してる)
			{
				this.On非活性化();
				this.r現在選択中の曲 = null;
				this.On活性化();
			}
		}


		/// <summary>
		/// 現在選曲している位置を検索する
		/// (曲一覧クラスを新しいものに入れ替える際に用いる)
		/// </summary>
		/// <param name="ln">検索対象のList</param>
		/// <param name="bc">検索するパンくずリスト(文字列)</param>
		/// <returns></returns>
		private C曲リストノード searchCurrentBreadcrumbsPosition(List<C曲リストノード> ln, string bc)
		{
			foreach (C曲リストノード n in ln)
			{
				if (n.strBreadcrumbs == bc)
				{
					return n;
				}
				else if (n.list子リスト != null && n.list子リスト.Count > 0)    // 子リストが存在するなら、再帰で探す
				{
					C曲リストノード r = searchCurrentBreadcrumbsPosition(n.list子リスト, bc);
					if (r != null) return r;
				}
			}
			return null;
		}

		/// <summary>
		/// BOXのアイテム数と、今何番目を選択しているかをセットする
		/// </summary>
		public void t選択曲が変更された(bool bForce) // #27648
		{
			C曲リストノード song = TJAPlayer3.stage選曲.act曲リスト.r現在選択中の曲;
			if (song == null)
				return;
			if (song == song_last && bForce == false)
				return;

			song_last = song;

			List<C曲リストノード> list = (song.r親ノード != null && !TJAPlayer3.ConfigIni.OpenOneSide) ? song.r親ノード.list子リスト : TJAPlayer3.SongsManager.list曲ルート;
			int index = list.IndexOf(song) + 1;
			if (index <= 0)
			{
				nCurrentPosition = nNumOfItems = 0;
			}
			else
			{
				int count = 0, current = 0;
				foreach (C曲リストノード node in list) 
				{
					if (node.eNodeType == C曲リストノード.ENodeType.SCORE)
						count++;
				}
				for (int cindex = 0; cindex < index; cindex++)
				{
					if (list[cindex].eNodeType == C曲リストノード.ENodeType.SCORE)
						current++;
				}
				nCurrentPosition = current;
				nNumOfItems = count;
			}
			TJAPlayer3.stage選曲.actHistoryPanel.tSongChange();
		}

		// CActivity 実装

		public override void On活性化()
		{
			if (this.b活性化してる)
				return;

			// Reset to not performing calibration each time we
			// enter or return to the song select screen.
			TJAPlayer3.IsPerformingCalibration = false;

			this.pfMusicName = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, 30);
			this.pfSubtitle = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, 23);

			this.n目標のスクロールカウンタ = 0;
			this.n現在のスクロールカウンタ = 0;
			this.nスクロールタイマ = -1;

			// フォント作成。
			// 曲リスト文字は２倍（面積４倍）でテクスチャに描画してから縮小表示するので、フォントサイズは２倍とする。

			FontStyle regular = FontStyle.Regular;

			// 現在選択中の曲がない（＝はじめての活性化）なら、現在選択中の曲をルートの先頭ノードに設定する。

			if ((this.r現在選択中の曲 == null) && (TJAPlayer3.SongsManager.list曲ルート.Count > 0))
				this.r現在選択中の曲 = TJAPlayer3.SongsManager.list曲ルート[0];


			// バー情報を初期化する。

			this.t現在選択中の曲を元に曲バーを再構成する();

			this.ct三角矢印アニメ = new CCounter();
			this.ct分岐フェード用タイマー = new CCounter(1, 2, 2500, TJAPlayer3.Timer);
			this.ctバー展開用タイマー= new CCounter(0, 100, 1, TJAPlayer3.Timer);
			this.ctバー展開ディレイ用タイマー = new CCounter(0, 200, 1, TJAPlayer3.Timer);

			base.On活性化();

			this.t選択曲が変更された(true);      // #27648 2012.3.31 yyagi 選曲画面に入った直後の 現在位置/全アイテム数 の表示を正しく行うため
		}
		public override void On非活性化()
		{
			if (this.b活性化してない)
				return;

			for (int i = 0; i < 13; i++) {
				this.stバー情報[i].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[i].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor);
			}

			TJAPlayer3.t安全にDisposeする(ref pfMusicName);
			TJAPlayer3.t安全にDisposeする(ref pfSubtitle);

			this.ttk選択している曲の曲名 = null;
			this.ttk選択している曲のサブタイトル = null;

			this.ct登場アニメ用 = null;

			this.ct三角矢印アニメ = null;



			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if (this.b活性化してない)
				return;

			int c = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja") ? 0 : 1;
			#region [ Songs not found画像 ]
			try
			{
				string[] s1 = { "曲データが見つかりません。\n曲データをDTXManiaGR.exe以下の\nフォルダにインストールして下さい。", "Songs not found.\nYou need to install songs." };

				using (CFontRenderer pffont = new CFontRenderer(TJAPlayer3.ConfigIni.FontName, 32))
				{
					this.txSongNotFound = TJAPlayer3.tCreateTexture(pffont.DrawText(s1[c], Color.White), true);
					this.txSongNotFound.vcScaling = new System.Numerics.Vector3(0.5f, 0.5f, 1f);
				}
			}
			catch (CTextureCreateFailedException e)
			{
				Trace.TraceError(e.ToString());
				Trace.TraceError("SoungNotFoundテクスチャの作成に失敗しました。");
				this.txSongNotFound = null;
			}
			#endregion
			#region [ "曲データを検索しています"画像 ]
			try
			{
				string[] s1 = { "曲データを検索しています。\nそのまましばらくお待ち下さい。", "Now enumerating songs.\nPlease wait..." };

				using (CFontRenderer pffont = new CFontRenderer(TJAPlayer3.ConfigIni.FontName, 32))
				{
					this.txEnumeratingSongs = TJAPlayer3.tCreateTexture(pffont.DrawText(s1[c], Color.White), true);
					this.txEnumeratingSongs.vcScaling = new System.Numerics.Vector3(0.5f, 0.5f, 1f);
				}
			}
			catch (CTextureCreateFailedException e)
			{
				Trace.TraceError(e.ToString());
				Trace.TraceError("txEnumeratingSongsテクスチャの作成に失敗しました。");
				this.txEnumeratingSongs = null;
			}
			#endregion
			#region [ 曲数表示 ]
			//this.txアイテム数数字 = CDTXMania.tCreateTexture( CSkin.Path( @"Graphics/ScreenSelect skill number on gauge etc.png" ), false );
			#endregion
			base.OnManagedリソースの作成();
		}
		public override void OnManagedリソースの解放()
		{
			if (this.b活性化してない)
				return;

			ClearTitleTextureCache();

			for (int i = 0; i < 13; i++)
			{
				this.stバー情報[i].ttkタイトル = null;
			}

			TJAPlayer3.t安全にDisposeする(ref this.txEnumeratingSongs);
			TJAPlayer3.t安全にDisposeする(ref this.txSongNotFound);

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
				this.ct登場アニメ用 = new CCounter(0, 100, 3, TJAPlayer3.Timer);

				this.nスクロールタイマ = (long)(CSoundManager.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
				TJAPlayer3.stage選曲.t選択曲変更通知();

				this.ct三角矢印アニメ.t開始(0, 1000, 1, TJAPlayer3.Timer);
				this.ct分岐フェード用タイマー.t進行();
				base.b初めての進行描画 = false;
			}
			//-----------------
			#endregion


			// まだ選択中の曲が決まってなければ、曲ツリールートの最初の曲にセットする。

			if ((this.r現在選択中の曲 == null) && (TJAPlayer3.SongsManager.list曲ルート.Count > 0))
				this.r現在選択中の曲 = TJAPlayer3.SongsManager.list曲ルート[0];

			// 本ステージは、(1)登場アニメフェーズ → (2)通常フェーズ　と二段階にわけて進む。

			//追加
			if (n現在のスクロールカウンタ == 0) this.ctバー展開ディレイ用タイマー.t進行();
			else { this.ctバー展開ディレイ用タイマー.n現在の値 = 0; this.ctバー展開ディレイ用タイマー.t時間Reset(); }

			if (ctバー展開ディレイ用タイマー.b終了値に達した) this.ct分岐フェード用タイマー.t進行Loop();
			else this.ct分岐フェード用タイマー.n現在の値 = 1;

			if (ctバー展開ディレイ用タイマー.b終了値に達した) this.ctバー展開用タイマー.t進行();
			else { this.ctバー展開用タイマー.n現在の値 = 0; this.ctバー展開用タイマー.t時間Reset(); }


			//展開アニメ用
			if (TJAPlayer3.Tx.SongSelect_Bar_Center != null)
				TJAPlayer3.Tx.SongSelect_Bar_Center.vcScaling.X = this.ctバー展開用タイマー.n現在の値 / 100f;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Box_Center_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Box_Center_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Box_Center_Genre[i].vcScaling.X = this.ctバー展開用タイマー.n現在の値 / 100f;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre[i].vcScaling.X = this.ctバー展開用タイマー.n現在の値 / 100f;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[i] != null)
				{
					TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[i].vcScaling.X = this.ctバー展開用タイマー.n現在の値 / 100f;
					TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[i].vcScaling.Y = Math.Max(this.ctバー展開用タイマー.n現在の値 - (this.ctバー展開用タイマー.n終了値 / 2), 0) / 50f;
				}
			//-----

			//DifficultySelectフェード用

			int 全体Opacity;
			if (TJAPlayer3.stage選曲.現在の選曲画面状況 == CStage選曲.E選曲画面.難易度選択In)
				全体Opacity = (int)(255.0f - (TJAPlayer3.stage選曲.ctDifficultySelectIN用タイマー.n現在の値 * 255.0f / TJAPlayer3.stage選曲.ctDifficultySelectIN用タイマー.n終了値));
			else if (TJAPlayer3.stage選曲.現在の選曲画面状況 == CStage選曲.E選曲画面.難易度選択Out)
				全体Opacity = (int)(TJAPlayer3.stage選曲.ctDifficultySelectOUT用タイマー.n現在の値 * 255.0f / TJAPlayer3.stage選曲.ctDifficultySelectOUT用タイマー.n終了値);
			else
				全体Opacity = 255;
			

			//しょうがないから、この曲リストで、描画するすべてのテクスチャのOpacityをここで決める。
			//その他のいい方法あったら、プルリクお願いします。受け入れます。(CActSelect曲リスト全体の透明度変更できたらいいな。)
			if (TJAPlayer3.Tx.SongSelect_Branch_Text_NEW != null)
				TJAPlayer3.Tx.SongSelect_Branch_Text_NEW.Opacity = Math.Min((int)((ct分岐フェード用タイマー.n現在の値 % 2) * 255.0), 全体Opacity);

			if (TJAPlayer3.Tx.SongSelect_PapaMama != null)
				TJAPlayer3.Tx.SongSelect_PapaMama.Opacity = Math.Min((int)((ct分岐フェード用タイマー.n現在の値 % 2) * 255.0), 全体Opacity);

			if (TJAPlayer3.Tx.SongSelect_Cursor_Left != null)
				TJAPlayer3.Tx.SongSelect_Cursor_Left.Opacity = Math.Min(255 - (ct三角矢印アニメ.n現在の値 * 255 / ct三角矢印アニメ.n終了値), 全体Opacity);

			if (TJAPlayer3.Tx.SongSelect_Cursor_Right != null)
				TJAPlayer3.Tx.SongSelect_Cursor_Right.Opacity = Math.Min(255 - (ct三角矢印アニメ.n現在の値 * 255 / ct三角矢印アニメ.n終了値), 全体Opacity);

			if (TJAPlayer3.Tx.SongSelect_Bar_Center != null)
				TJAPlayer3.Tx.SongSelect_Bar_Center.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.SongSelect_Frame_Score != null)
				TJAPlayer3.Tx.SongSelect_Frame_Score.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.SongSelect_Frame_BackBox != null)
				TJAPlayer3.Tx.SongSelect_Frame_BackBox.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.SongSelect_Frame_Random != null)
				TJAPlayer3.Tx.SongSelect_Frame_Random.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.SongSelect_Level != null)
				TJAPlayer3.Tx.SongSelect_Level.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.Crown_t != null)
				TJAPlayer3.Tx.Crown_t.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.DanC_Crown_t != null)
				TJAPlayer3.Tx.DanC_Crown_t.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.Difficulty_Icons != null)
				TJAPlayer3.Tx.Difficulty_Icons.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.SongSelect_GenreText != null)
				TJAPlayer3.Tx.SongSelect_GenreText.Opacity = 全体Opacity;

			if (TJAPlayer3.Tx.SongSelect_Bar_BackBox != null)
				TJAPlayer3.Tx.SongSelect_Bar_BackBox.Opacity = 全体Opacity;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Lyric_Text.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Lyric_Text[i] != null)
					TJAPlayer3.Tx.SongSelect_Lyric_Text[i].Opacity = 全体Opacity;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[i].Opacity = 全体Opacity;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Box_Center_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Box_Center_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Box_Center_Genre[i].Opacity = 全体Opacity;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[i].Opacity = 全体Opacity;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre[i].Opacity = 全体Opacity;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Bar_Box_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Bar_Box_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Bar_Box_Genre[i].Opacity = 全体Opacity;

			for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Bar_Genre.Length; i++)
				if (TJAPlayer3.Tx.SongSelect_Bar_Genre[i] != null)
					TJAPlayer3.Tx.SongSelect_Bar_Genre[i].Opacity = 全体Opacity;
			//------------ぐちゃぐちゃで意味わからんよね。ごめんね。
			//こう見てみると、意外にテクスチャ少なめ？


			// 進行。
			if (n現在のスクロールカウンタ == 0) ct三角矢印アニメ.t進行Loop();
			else ct三角矢印アニメ.n現在の値 = 0;


			if (!this.ct登場アニメ用.b終了値に達した)
			{
				#region [ (1) 登場アニメフェーズの進行。]
				//-----------------
				this.ct登場アニメ用.t進行();
				//-----------------
				#endregion
			}
			else
			{
				#region [ (2) 通常フェーズの進行。]
				//-----------------
				long n現在時刻 = CSoundManager.rc演奏用タイマ.n現在時刻ms;

				if (n現在時刻 < this.nスクロールタイマ) // 念のため
					this.nスクロールタイマ = n現在時刻;

				const int nアニメ間隔 = 2;
				while ((n現在時刻 - this.nスクロールタイマ) >= nアニメ間隔)
				{
					int n加速度 = 1;
					int n残距離 = Math.Abs((int)(this.n目標のスクロールカウンタ - this.n現在のスクロールカウンタ));

					#region [ 残距離が遠いほどスクロールを速くする（＝n加速度を多くする）。]
					//-----------------
					if (n残距離 <= 10)
					{
						n加速度 = 1;
					}
					else if (n残距離 <= 100)
					{
						n加速度 = 2;
					}
					else if (n残距離 <= 300)
					{
						n加速度 = 3;
					}
					else if (n残距離 <= 500)
					{
						n加速度 = 4;
					}
					else
					{
						n加速度 = 8;
					}
					//-----------------
					#endregion

					#region [ 加速度を加算し、現在のスクロールカウンタを目標のスクロールカウンタまで近づける。 ]
					//-----------------
					if (this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ)        // (A) 正の方向に未達の場合：
					{
						this.n現在のスクロールカウンタ += n加速度;                             // カウンタを正方向に移動する。

						if (this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ)
							this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;    // 到着！スクロール停止！
					}

					else if (this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ)   // (B) 負の方向に未達の場合：
					{
						this.n現在のスクロールカウンタ -= n加速度;                             // カウンタを負方向に移動する。

						if (this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ)    // 到着！スクロール停止！
							this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;
					}
					//-----------------
					#endregion

					if (this.n現在のスクロールカウンタ >= 100)      // １行＝100カウント。
					{
						#region [ パネルを１行上にシフトする。]
						//-----------------

						// 選択曲と選択行を１つ下の行に移動。

						this.r現在選択中の曲 = this.r次の曲(this.r現在選択中の曲);
						this.n現在の選択行 = (this.n現在の選択行 + 1) % 13;


						// 選択曲から７つ下のパネル（＝新しく最下部に表示されるパネル。消えてしまう一番上のパネルを再利用する）に、新しい曲の情報を記載する。

						C曲リストノード song = this.r現在選択中の曲;
						for (int i = 0; i < 6; i++)
							song = this.r次の曲(song);

						int index = (this.n現在の選択行 + 6) % 13;    // 新しく最下部に表示されるパネルのインデックス（0～12）。
						this.stバー情報[index].strタイトル文字列 = song.strTitle;
						this.stバー情報[index].strSubTitle = song.arスコア.譜面情報.strSubTitle;
						this.stバー情報[index].ar難易度 = song.arスコア.譜面情報.nレベル;
						this.stバー情報[index].b分岐 = song.arスコア.譜面情報.b譜面分岐;
						this.stバー情報[index].n王冠 = song.arスコア.譜面情報.n王冠;
						this.stバー情報[index].ForeColor = song.ForeColor;
						this.stバー情報[index].BackColor = song.BackColor;
						this.stバー情報[index].song = song;


						// stバー情報[] の内容を1行ずつずらす。

						C曲リストノード song2 = this.r現在選択中の曲;
						for (int i = 0; i < 6; i++)
							song2 = this.r前の曲(song2);

						for (int i = 0; i < 13; i++)
						{
							int n = (((this.n現在の選択行 - 6) + i) + 13) % 13;
							this.stバー情報[n].song = song2;
							this.stバー情報[n].eNodeType = song2.eNodeType;
							song2 = this.r次の曲(song2);
							this.stバー情報[i].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[i].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor);

						}

						// 1行(100カウント)移動完了。

						this.n現在のスクロールカウンタ -= 100;
						this.n目標のスクロールカウンタ -= 100;

						this.t選択曲が変更された(false);             // スクロールバー用に今何番目を選択しているかを更新

						this.ttk選択している曲の曲名 = null;
						this.ttk選択している曲のサブタイトル = null;


						if (this.n目標のスクロールカウンタ == 0)
							TJAPlayer3.stage選曲.t選択曲変更通知();      // スクロール完了＝選択曲変更！

						//-----------------
						#endregion
					}
					else if (this.n現在のスクロールカウンタ <= -100)
					{
						#region [ パネルを１行下にシフトする。]
						//-----------------

						// 選択曲と選択行を１つ上の行に移動。

						this.r現在選択中の曲 = this.r前の曲(this.r現在選択中の曲);
						this.n現在の選択行 = ((this.n現在の選択行 - 1) + 13) % 13;


						// 選択曲から５つ上のパネル（＝新しく最上部に表示されるパネル。消えてしまう一番下のパネルを再利用する）に、新しい曲の情報を記載する。

						C曲リストノード song = this.r現在選択中の曲;
						for (int i = 0; i < 6; i++)
							song = this.r前の曲(song);

						int index = ((this.n現在の選択行 - 6) + 13) % 13; // 新しく最上部に表示されるパネルのインデックス（0～12）。
						this.stバー情報[index].strタイトル文字列 = song.strTitle;
						this.stバー情報[index].strSubTitle = song.arスコア.譜面情報.strSubTitle;
						this.stバー情報[index].ar難易度 = song.arスコア.譜面情報.nレベル;
						this.stバー情報[index].b分岐 = song.arスコア.譜面情報.b譜面分岐;
						this.stバー情報[index].n王冠 = song.arスコア.譜面情報.n王冠;
						this.stバー情報[index].ForeColor = song.ForeColor;
						this.stバー情報[index].BackColor = song.BackColor;
						this.stバー情報[index].song = song;

						// stバー情報[] の内容を1行ずつずらす。

						C曲リストノード song2 = this.r現在選択中の曲;
						for (int i = 0; i < 6; i++)
							song2 = this.r前の曲(song2);

						for (int i = 0; i < 13; i++)
						{
							int n = (((this.n現在の選択行 - 6) + i) + 13) % 13;
							this.stバー情報[n].song = song2;
							this.stバー情報[n].eNodeType = song2.eNodeType;
							song2 = this.r次の曲(song2);
							this.stバー情報[i].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[i].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor);
						}

						// 1行(100カウント)移動完了。

						this.n現在のスクロールカウンタ += 100;
						this.n目標のスクロールカウンタ += 100;

						this.t選択曲が変更された(false);             // スクロールバー用に今何番目を選択しているかを更新

						this.ttk選択している曲の曲名 = null;
						this.ttk選択している曲のサブタイトル = null;

						if (this.n目標のスクロールカウンタ == 0)
							TJAPlayer3.stage選曲.t選択曲変更通知();      // スクロール完了＝選択曲変更！
																//-----------------
						#endregion
					}

					if (this.b選択曲が変更された && n現在のスクロールカウンタ == 0)
					{
						if (this.ttk選択している曲の曲名 != null)
						{
							this.ttk選択している曲の曲名 = null;
							this.b選択曲が変更された = false;
						}
						if (this.ttk選択している曲のサブタイトル != null)
						{
							this.ttk選択している曲のサブタイトル = null;
							this.b選択曲が変更された = false;
						}
					}
					this.nスクロールタイマ += nアニメ間隔;
				}
				//-----------------
				#endregion
			}


			// 描画。
			if (this.r現在選択中の曲 == null)
			{
				#region [ 曲が１つもないなら「Songs not found.」を表示してここで帰れ。]
				//-----------------
				if (bIsEnumeratingSongs)
				{
					if (this.txEnumeratingSongs != null)
					{
						this.txEnumeratingSongs.t2D描画(TJAPlayer3.app.Device, 320, 160);
					}
				}
				else
				{
					if (this.txSongNotFound != null)
						this.txSongNotFound.t2D描画(TJAPlayer3.app.Device, 320, 160);
				}
				if (TJAPlayer3.InputManager.Keyboard.bIsKeyPressed((int)SlimDXKeys.Key.Escape))
				{
					TJAPlayer3.Skin.sound取消音.t再生する();
					TJAPlayer3.stage選曲.eFadeOut完了時の戻り値 = CStage選曲.E戻り値.タイトルに戻る;
					TJAPlayer3.stage選曲.actFIFO.tFadeOut開始();
					TJAPlayer3.stage選曲.eフェーズID = CStage.Eフェーズ.共通_FadeOut;
					return 0;
				}
				//-----------------
				#endregion

				return 0;
			}

			if (this.r現在選択中の曲.r親ノード != null)
			{
				int GenreBack = TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.r親ノード.strGenre);
				if (TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[GenreBack] != null && TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[GenreBack] != null && !TJAPlayer3.ConfigIni.OpenOneSide)
				{
					int ForLoop = (int)(1280 / 100) + 1;
					for (int i = 0; i < ForLoop; i++)
						TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[GenreBack].t2D描画(TJAPlayer3.app.Device, i * 100, TJAPlayer3.Skin.SongSelect_Overall_Y - 69, new Rectangle(100, 0, 100, TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[GenreBack].szTextureSize.Height));
					TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[GenreBack].vcScaling = new Vector3(1f);
					TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[GenreBack].t2D元サイズ基準描画(TJAPlayer3.app.Device, CTexture.RefPnt.Center, BoxCenterx, TJAPlayer3.Skin.SongSelect_Overall_Y + TJAPlayer3.Skin.SongSelect_Box_Center_Header_Y_Diff - 19, new Rectangle(0, 0, TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[GenreBack].szTextureSize.Width, 62));
				}
			}

			int nnGenreBack = TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.strGenre);

			#region [ (2) 通常フェーズの描画。]
			//-----------------
			#region[片開き用の背景]
			if (TJAPlayer3.ConfigIni.OpenOneSide)
			{
				for (int i = 0; i < 13; i++)
				{
					if ((i == 0 && this.n現在のスクロールカウンタ > 0) ||       // 最上行は、上に移動中なら表示しない。
						(i == 12 && this.n現在のスクロールカウンタ < 0))        // 最下行は、下に移動中なら表示しない。
						continue;

					int nパネル番号 = (((this.n現在の選択行 - 6) + i) + 13) % 13;
					int n見た目の行番号 = i;
					int n次のパネル番号 = (this.n現在のスクロールカウンタ <= 0) ? ((i + 1) % 13) : (((i - 1) + 13) % 13);
					int xAnime = this.ptバーの座標[n見た目の行番号].X + ((int)((this.ptバーの座標[n次のパネル番号].X - this.ptバーの座標[n見た目の行番号].X) * (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));

					if (n見た目の行番号 == 5 && this.stバー情報[(nパネル番号 + 1) % 13].song.r親ノード != null) //5のところでCenterを描画しないと、選択曲変更のとき、背景に隠れてしまう。
					{
						int genre = TJAPlayer3.Skin.nStrジャンルtoNum(this.stバー情報[(nパネル番号 + 1) % 13].song.r親ノード.strGenre);
						int basho = 1;
						if (this.stバー情報[(nパネル番号 + 1) % 13].song.r親ノード.list子リスト.IndexOf(this.stバー情報[(nパネル番号 + 1) % 13].song) == 0)
							basho = 0;
						else if (this.stバー情報[(nパネル番号 + 1) % 13].song.r親ノード.list子リスト.IndexOf(this.stバー情報[(nパネル番号 + 1) % 13].song) == (this.stバー情報[(nパネル番号 + 1) % 13].song.r親ノード.list子リスト.Count - 1))
							basho = 2;

						int sixbasho = basho;
						int ForLoop;
						ForLoop = Math.Abs((this.ptバーの座標[5].X + 100) - this.ptバーの座標[7].X) / 100;
						for (int lo = 0; lo < ForLoop; lo++)
						{
							if (basho == 0)
							{
								if (lo == 0)
								{
									sixbasho = 0;
								}
								else
								{
									sixbasho = 1;
								}
							}
							else if (basho == 2)
							{
								if (lo == ForLoop - 1)
								{
									sixbasho = 2;
								}
								else
								{
									sixbasho = 1;
								}
							}
							int sixx = this.ptバーの座標[5].X + 100 + lo * 100;
							sixx -= this.n現在のスクロールカウンタ;
							if (TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[genre] != null)
								TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[genre].t2D描画(TJAPlayer3.app.Device, sixx, TJAPlayer3.Skin.SongSelect_Overall_Y - 69, new Rectangle(sixbasho * 100, 0, 100, TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[nnGenreBack].szTextureSize.Height));
						}
					}

					if (this.stバー情報[nパネル番号].song.r親ノード != null)
					{
						int genre = TJAPlayer3.Skin.nStrジャンルtoNum(this.stバー情報[nパネル番号].song.r親ノード.strGenre);
						int basho = 1;
						if (this.stバー情報[nパネル番号].song.r親ノード.list子リスト.IndexOf(this.stバー情報[nパネル番号].song) == 0)
							basho = 0;
						else if (this.stバー情報[nパネル番号].song.r親ノード.list子リスト.IndexOf(this.stバー情報[nパネル番号].song) == (this.stバー情報[nパネル番号].song.r親ノード.list子リスト.Count - 1))
							basho = 2;
						if (n見た目の行番号 != 5 && n見た目の行番号 != 6 && n見た目の行番号 != 7)
						{
							if (TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[genre] != null)
								TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[genre].t2D描画(TJAPlayer3.app.Device, xAnime, TJAPlayer3.Skin.SongSelect_Overall_Y - 69, new Rectangle(basho * 100, 0, 100, TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[nnGenreBack].szTextureSize.Height));
						}
						else if (n見た目の行番号 != 6)
						{
							int fivesevenx = this.ptバーの座標[n見た目の行番号].X;
							fivesevenx -= this.n現在のスクロールカウンタ;
							if (TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[genre] != null)
								TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[genre].t2D描画(TJAPlayer3.app.Device, fivesevenx, TJAPlayer3.Skin.SongSelect_Overall_Y - 69, new Rectangle(basho * 100, 0, 100, TJAPlayer3.Tx.SongSelect_Bar_Center_Back_Genre[nnGenreBack].szTextureSize.Height));
						}
					}
				}

				if (this.r現在選択中の曲.r親ノード != null)
				{
					int genreheader = TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.r親ノード.strGenre);
					if (TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[genreheader] != null)
					{
						TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[genreheader].vcScaling = new Vector3(1f);
						TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[genreheader].t2D元サイズ基準描画(TJAPlayer3.app.Device, CTexture.RefPnt.Center, BoxCenterx, TJAPlayer3.Skin.SongSelect_Overall_Y + TJAPlayer3.Skin.SongSelect_Box_Center_Header_Y_Diff - 19, new Rectangle(0, 0, TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[nnGenreBack].szTextureSize.Width, 62));
					}
				}
			}
			#endregion
			for (int i = 0; i < 13; i++)    // パネルは全13枚。
			{
				if ((i == 0 && this.n現在のスクロールカウンタ > 0) ||       // 最上行は、上に移動中なら表示しない。
					(i == 12 && this.n現在のスクロールカウンタ < 0))        // 最下行は、下に移動中なら表示しない。
					continue;

				int nパネル番号 = (((this.n現在の選択行 - 6) + i) + 13) % 13;
				int n見た目の行番号 = i;
				int n次のパネル番号 = (this.n現在のスクロールカウンタ <= 0) ? ((i + 1) % 13) : (((i - 1) + 13) % 13);
				int xAnime = this.ptバーの座標[n見た目の行番号].X + ((int)((this.ptバーの座標[n次のパネル番号].X - this.ptバーの座標[n見た目の行番号].X) * (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));

				#region [曲決定時のバーの横移動]
                if (TJAPlayer3.stage選曲.現在の選曲画面状況 == CStage選曲.E選曲画面.難易度選択Out)
				{
					if (n見た目の行番号 < 6)
						xAnime += (TJAPlayer3.stage選曲.ctDifficultySelectOUT用タイマー.n現在の値 - TJAPlayer3.stage選曲.ctDifficultySelectOUT用タイマー.n終了値) * 3;
					else if (n見た目の行番号 > 6)
						xAnime -= (TJAPlayer3.stage選曲.ctDifficultySelectOUT用タイマー.n現在の値 - TJAPlayer3.stage選曲.ctDifficultySelectOUT用タイマー.n終了値) * 3;
				}
				else if (TJAPlayer3.stage選曲.現在の選曲画面状況 == CStage選曲.E選曲画面.難易度選択In)
				{
					if (n見た目の行番号 < 6)
						xAnime -= TJAPlayer3.stage選曲.ctDifficultySelectIN用タイマー.n現在の値 * 3;
					else if (n見た目の行番号 > 6)
						xAnime += TJAPlayer3.stage選曲.ctDifficultySelectIN用タイマー.n現在の値 * 3;
				}
				#endregion

				{
					// (B) スクロール中の選択曲バー、またはその他のバーの描画。

					#region [ バーテクスチャを描画。]
					//-----------------
					if (n現在のスクロールカウンタ != 0)
						this.tジャンル別選択されていない曲バーの描画(xAnime, TJAPlayer3.Skin.SongSelect_Overall_Y, TJAPlayer3.Skin.nStrジャンルtoNum(this.stバー情報[nパネル番号].song.strGenre), this.stバー情報[nパネル番号].eNodeType);
					else if (n見た目の行番号 != 6 || !(ctバー展開ディレイ用タイマー.b終了値に達した))
						this.tジャンル別選択されていない曲バーの描画(xAnime, TJAPlayer3.Skin.SongSelect_Overall_Y, TJAPlayer3.Skin.nStrジャンルtoNum(this.stバー情報[nパネル番号].song.strGenre), this.stバー情報[nパネル番号].eNodeType);
					//-----------------
					#endregion

					#region [ タイトル名テクスチャを描画。]
					int BackBoxDiff = (this.stバー情報[nパネル番号].eNodeType == C曲リストノード.ENodeType.BACKBOX) ? TJAPlayer3.Skin.SongSelect_BackBoxText_Y_Diff : 0;

					if (n現在のスクロールカウンタ != 0 || n見た目の行番号 != 6 || !(ctバー展開ディレイ用タイマー.b終了値に達した))
						ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル).t2D描画(TJAPlayer3.app.Device, xAnime + 28, TJAPlayer3.Skin.SongSelect_Overall_Y + 23 + BackBoxDiff);
                    #endregion

                    #region [王冠を描画。]
                    bool DanJudge = false;
					if (TJAPlayer3.Tx.Crown_t != null && TJAPlayer3.Tx.DanC_Crown_t != null && (n見た目の行番号 != 6 || !ctバー展開ディレイ用タイマー.b終了値に達した) && this.stバー情報[nパネル番号].eNodeType == C曲リストノード.ENodeType.SCORE)
					{
						if (this.stバー情報[nパネル番号].n王冠[(int)Difficulty.Dan] != 0)
						{
							TJAPlayer3.Tx.DanC_Crown_t.vcScaling = new Vector3(0.75f);
							TJAPlayer3.Tx.DanC_Crown_t.t2D描画(TJAPlayer3.app.Device, xAnime + 30, TJAPlayer3.Skin.SongSelect_Overall_Y - 30, new Rectangle((this.stバー情報[nパネル番号].n王冠[(int)Difficulty.Dan] - 1) * 50, 0, 50, 100));
							DanJudge = true;
						}
						TJAPlayer3.Tx.Crown_t.vcScaling.X = 0.5f;
						TJAPlayer3.Tx.Crown_t.vcScaling.Y = 0.5f;
						for (int j = 4; j >= 0; j--)
						{
							if (DanJudge)//2020.05.25 Mr-Ojii 汚いかもしれないけど、gotoを使わないでやるにはこうするしか思いつかなかった。
								break;
							if (this.stバー情報[nパネル番号].n王冠[j] != 0)
							{
								TJAPlayer3.Tx.Crown_t.t2D描画(TJAPlayer3.app.Device, xAnime + 25, TJAPlayer3.Skin.SongSelect_Overall_Y - 23, new Rectangle(this.stバー情報[nパネル番号].n王冠[j] * 100, 0, 100, 100));
								if (TJAPlayer3.Tx.Difficulty_Icons != null)
								{
									TJAPlayer3.Tx.Difficulty_Icons.vcScaling.X = 0.4f;
									TJAPlayer3.Tx.Difficulty_Icons.vcScaling.Y = 0.4f;
									TJAPlayer3.Tx.Difficulty_Icons.t2D描画(TJAPlayer3.app.Device, xAnime + 40, TJAPlayer3.Skin.SongSelect_Overall_Y - 15, new Rectangle(j * 100, 0, 100, 100));
								}
								break;
							}
						}
					}
					#endregion
				}
			}
			#endregion

			if ((this.n現在のスクロールカウンタ == 0) && (ctバー展開ディレイ用タイマー.b終了値に達した))
			{
				nnGenreBack = TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.strGenre);
				if (TJAPlayer3.Tx.SongSelect_Box_Center_Genre[nnGenreBack] != null && TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[nnGenreBack] != null && this.r現在選択中の曲.eNodeType != C曲リストノード.ENodeType.BACKBOX && this.r現在選択中の曲.eNodeType != C曲リストノード.ENodeType.RANDOM)
				{
					if (this.r現在選択中の曲.eNodeType != C曲リストノード.ENodeType.SCORE)
					{
						TJAPlayer3.Tx.SongSelect_Box_Center_Genre[nnGenreBack].t2D描画(TJAPlayer3.app.Device, BoxCenterx - (int)(TJAPlayer3.Tx.SongSelect_Box_Center_Genre[nnGenreBack].szTextureSize.Width / 2 * ctバー展開用タイマー.n現在の値 / 100.0), TJAPlayer3.Skin.SongSelect_Overall_Y);
						if (TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre[nnGenreBack] != null)
							TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre[nnGenreBack].t2D描画(TJAPlayer3.app.Device, BoxCenterx - (int)(TJAPlayer3.Tx.SongSelect_Box_Center_Text_Genre[nnGenreBack].szTextureSize.Width / 2 * ctバー展開用タイマー.n現在の値 / 100.0), TJAPlayer3.Skin.SongSelect_Overall_Y);
						TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[nnGenreBack].t2D描画(TJAPlayer3.app.Device, BoxCenterx - (int)(TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[nnGenreBack].szTextureSize.Width / 2 * ctバー展開用タイマー.n現在の値 / 100.0), TJAPlayer3.Skin.SongSelect_Overall_Y + TJAPlayer3.Skin.SongSelect_Box_Center_Header_Y_Diff - 69 + TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[nnGenreBack].szTextureSize.Height / 2 - (int)(TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[nnGenreBack].szTextureSize.Height / 2 * Math.Max(this.ctバー展開用タイマー.n現在の値 - (this.ctバー展開用タイマー.n終了値 / 2), 0) / 50.0));
					}
					else if (TJAPlayer3.Tx.SongSelect_Bar_Center != null)
						TJAPlayer3.Tx.SongSelect_Bar_Center.t2D描画(TJAPlayer3.app.Device, BoxCenterx - (int)(TJAPlayer3.Tx.SongSelect_Bar_Center.szTextureSize.Width / 2 * ctバー展開用タイマー.n現在の値 / 100.0), TJAPlayer3.Skin.SongSelect_Overall_Y);
				}
				else if (TJAPlayer3.Tx.SongSelect_Bar_Center != null)
					TJAPlayer3.Tx.SongSelect_Bar_Center.t2D描画(TJAPlayer3.app.Device, BoxCenterx - (int)(TJAPlayer3.Tx.SongSelect_Bar_Center.szTextureSize.Width / 2 * ctバー展開用タイマー.n現在の値 / 100.0), TJAPlayer3.Skin.SongSelect_Overall_Y);

				if (this.r現在選択中の曲.eNodeType == C曲リストノード.ENodeType.BOX)
				{
					int genretextureindex = TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.strGenre);
					if (TJAPlayer3.Tx.SongSelect_GenreText != null)
					{
						TJAPlayer3.Tx.SongSelect_GenreText.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Center, BoxCenterx, TJAPlayer3.Skin.SongSelect_Overall_Y - 25, new Rectangle(0, genretextureindex * (TJAPlayer3.Tx.SongSelect_GenreText.szTextureSize.Height / (TJAPlayer3.Skin.MaxKeyNum + 2)), TJAPlayer3.Tx.SongSelect_GenreText.szTextureSize.Width, (TJAPlayer3.Tx.SongSelect_GenreText.szTextureSize.Height / (TJAPlayer3.Skin.MaxKeyNum + 2))));
					}
				}
				switch (r現在選択中の曲.eNodeType)
				{
					case C曲リストノード.ENodeType.SCORE:
						{
                            #region [ Frame ]
                            if (TJAPlayer3.Tx.SongSelect_Frame_Score != null)
							{
								// 難易度がTower、Danではない
								if (TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Dan)
								{
									for (int i = 0; i < (int)Difficulty.Edit + 1; i++)
									{
										if (r現在選択中のスコア.譜面情報.b譜面が存在する[i])
										{
											// レベルが0以上
											TJAPlayer3.Tx.SongSelect_Frame_Score.color = Color.FromArgb(255, 255, 255, 255);
											if (i == 4 && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == 4)
											{
												// エディット
												TJAPlayer3.Tx.SongSelect_Frame_Score.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494 + (3 * 60), TJAPlayer3.Skin.SongSelect_Overall_Y + 463, new Rectangle(60 * i, 0, 60, 360));
											}
											else if (i != 4)
											{
												TJAPlayer3.Tx.SongSelect_Frame_Score.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494 + (i * 60), TJAPlayer3.Skin.SongSelect_Overall_Y + 463, new Rectangle(60 * i, 0, 60, 360));
											}
										}
										else
										{
											// レベルが0未満 = 譜面がないとみなす
											TJAPlayer3.Tx.SongSelect_Frame_Score.color = Color.FromArgb(255, 127, 127, 127);
											if (i == 4 && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == 4)
											{
												// エディット
												TJAPlayer3.Tx.SongSelect_Frame_Score.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494 + (3 * 60), TJAPlayer3.Skin.SongSelect_Overall_Y + 463, new Rectangle(60 * i, 0, 60, 360));
											}
											else if (i != 4)
											{
												TJAPlayer3.Tx.SongSelect_Frame_Score.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494 + (i * 60), TJAPlayer3.Skin.SongSelect_Overall_Y + 463, new Rectangle(60 * i, 0, 60, 360));
											}
										}
									}
								}
								else
								{
									if (r現在選択中のスコア.譜面情報.b譜面が存在する[TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0]])
										TJAPlayer3.Tx.SongSelect_Frame_Score.color = Color.FromArgb(255, 255, 255, 255);
									else
										TJAPlayer3.Tx.SongSelect_Frame_Score.color = Color.FromArgb(255, 127, 127, 127);

									TJAPlayer3.Tx.SongSelect_Frame_Score.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494 + 120, TJAPlayer3.Skin.SongSelect_Overall_Y + 463, new Rectangle(0, 360 + (360 * (TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] - (int)Difficulty.Tower)), TJAPlayer3.Tx.SongSelect_Frame_Score.szTextureSize.Width, 360));
								}
							}
							#endregion
							#region[ 星 ]
							if (TJAPlayer3.Tx.SongSelect_Level != null)
							{
								// 全難易度表示
								// 難易度がTower、Danではない
								if (TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Dan)
								{
									for (int i = 0; i < (int)Difficulty.Edit + 1; i++)
									{
										for (int n = 0; n < this.r現在選択中のスコア.譜面情報.nレベル[i]; n++)
										{
											// 星11以上はループ終了
											//if (n > 9) break;
											// 裏なら鬼と同じ場所に
											if (i == 3 && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == 4) break;
											if (i == 4 && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == 4)
											{
												TJAPlayer3.Tx.SongSelect_Level.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494 + (3 * 60), TJAPlayer3.Skin.SongSelect_Overall_Y + 413 - (n * 17), new Rectangle(32 * i, 0, 32, 32));
											}
											if (i != 4)
											{
												TJAPlayer3.Tx.SongSelect_Level.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494 + (i * 60), TJAPlayer3.Skin.SongSelect_Overall_Y + 413 - (n * 17), new Rectangle(32 * i, 0, 32, 32));
											}
										}
									}
								}
								else
								{
									for (int i = 0; i < this.r現在選択中のスコア.譜面情報.nレベル[TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0]]; i++)
									{
										TJAPlayer3.Tx.SongSelect_Level.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 494, TJAPlayer3.Skin.SongSelect_Overall_Y + 413 - (i * 17), new Rectangle(32 * TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0], 0, 32, 32));
									}
								}
							}
							#endregion
							#region[譜面分岐や歌詞やパパママ]
							if (TJAPlayer3.Tx.SongSelect_Lyric_Text[TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.strGenre)] != null && this.r現在選択中のスコア.譜面情報.b歌詞あり)
								TJAPlayer3.Tx.SongSelect_Lyric_Text[TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.strGenre)].t2D描画(TJAPlayer3.app.Device, 483, TJAPlayer3.Skin.SongSelect_Overall_Y + 21);

							if (TJAPlayer3.Tx.SongSelect_Branch_Text_NEW != null && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Dan)
							{
								for (int i = 0; i < 4; i++)
								{
									if (i == 3 && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == 4)
									{
										if (this.r現在選択中のスコア.譜面情報.b譜面分岐[4])
										{
											TJAPlayer3.Tx.SongSelect_Branch_Text_NEW.t2D描画(TJAPlayer3.app.Device, i * 60 + 479, TJAPlayer3.Skin.SongSelect_Overall_Y + 234, new Rectangle(32, 0, 32, 180));
										}
									}
									else
									{
										if (this.r現在選択中のスコア.譜面情報.b譜面分岐[i])
										{
											TJAPlayer3.Tx.SongSelect_Branch_Text_NEW.t2D描画(TJAPlayer3.app.Device, i * 60 + 479, TJAPlayer3.Skin.SongSelect_Overall_Y + 234, new Rectangle(0, 0, 32, 180));
										}
									}
								}
							}
							if (TJAPlayer3.Tx.SongSelect_PapaMama != null && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] != (int)Difficulty.Dan)
							{
								for (int i = 0; i < 4; i++)
								{
									if (i == 3 && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == 4)
									{
										if (this.r現在選択中のスコア.譜面情報.bPapaMamaSupport[4])
										{
											TJAPlayer3.Tx.SongSelect_PapaMama.t2D描画(TJAPlayer3.app.Device, i * 60 + 479, TJAPlayer3.Skin.SongSelect_Overall_Y + 234, new Rectangle(32, 0, 32, 180));
										}
									}
									else
									{
										if (this.r現在選択中のスコア.譜面情報.bPapaMamaSupport[i])
										{
											TJAPlayer3.Tx.SongSelect_PapaMama.t2D描画(TJAPlayer3.app.Device, i * 60 + 479, TJAPlayer3.Skin.SongSelect_Overall_Y + 234, new Rectangle(0, 0, 32, 180));
										}
									}
								}
							}
							#endregion
							#region[王冠]
							if (TJAPlayer3.Tx.Crown_t != null && TJAPlayer3.Tx.DanC_Crown_t != null)
							{
								TJAPlayer3.Tx.Crown_t.vcScaling.X = 0.25f;
								TJAPlayer3.Tx.Crown_t.vcScaling.Y = 0.25f;
								if (TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] <= 4)
								{
									for (int i = 0; i < 4; i++)
									{
										if (TJAPlayer3.Tx.Crown_t != null && this.r現在選択中のスコア.譜面情報.n王冠[i] >= 0 && this.r現在選択中のスコア.譜面情報.n王冠[i] <= 3)
										{
											if (i == 3 && TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == 4)
											{
												TJAPlayer3.Tx.Crown_t.t2D描画(TJAPlayer3.app.Device, i * 60 + 482, TJAPlayer3.Skin.SongSelect_Overall_Y + 85, new Rectangle((this.r現在選択中のスコア.譜面情報.n王冠[4]) * 100, 0, 100, 100));
											}
											else if (this.r現在選択中のスコア.譜面情報.b譜面が存在する[i])
											{
												TJAPlayer3.Tx.Crown_t.t2D描画(TJAPlayer3.app.Device, i * 60 + 482, TJAPlayer3.Skin.SongSelect_Overall_Y + 85, new Rectangle((this.r現在選択中のスコア.譜面情報.n王冠[i]) * 100, 0, 100, 100));
											}
										}
									}
								}
								else if (TJAPlayer3.stage選曲.n現在選択中の曲の難易度[0] == (int)Difficulty.Tower)
								{
									TJAPlayer3.Tx.Crown_t.t2D描画(TJAPlayer3.app.Device, 482, TJAPlayer3.Skin.SongSelect_Overall_Y + 57, new Rectangle((this.r現在選択中のスコア.譜面情報.n王冠[5]) * 100, 0, 100, 100));
								}
								else
								{
									TJAPlayer3.Tx.DanC_Crown_t.vcScaling = new Vector3(1f);
									if (this.r現在選択中のスコア.譜面情報.n王冠[(int)Difficulty.Dan] != 0)
										TJAPlayer3.Tx.DanC_Crown_t.t2D描画(TJAPlayer3.app.Device, 482, TJAPlayer3.Skin.SongSelect_Overall_Y + 20, new Rectangle((this.r現在選択中のスコア.譜面情報.n王冠[(int)Difficulty.Dan] - 1) * 50, 0, 50, 100));
								}
							}
							#endregion
						}
						break;

					case C曲リストノード.ENodeType.BOX:
						if (TJAPlayer3.Tx.SongSelect_Frame_Box != null)
							TJAPlayer3.Tx.SongSelect_Frame_Box.t2D描画(TJAPlayer3.app.Device, 450, TJAPlayer3.Skin.SongSelect_Overall_Y);
						break;

					case C曲リストノード.ENodeType.BACKBOX:
						if (TJAPlayer3.Tx.SongSelect_Frame_BackBox != null)
							TJAPlayer3.Tx.SongSelect_Frame_BackBox.t2D描画(TJAPlayer3.app.Device, 450, TJAPlayer3.Skin.SongSelect_Overall_Y);
						break;

					case C曲リストノード.ENodeType.RANDOM:
						if (TJAPlayer3.Tx.SongSelect_Frame_Random != null)
							TJAPlayer3.Tx.SongSelect_Frame_Random.t2D描画(TJAPlayer3.app.Device, 450, TJAPlayer3.Skin.SongSelect_Overall_Y);
						break;
				}

			}

			#region [ 項目リストにフォーカスがあって、かつスクロールが停止しているなら、パネルの上下に▲印を描画する。]
			//-----------------
			if ((this.n目標のスクロールカウンタ == 0))
			{
				int Cursor_L = 372 - this.ct三角矢印アニメ.n現在の値 / 50;
				int Cursor_R = 819 + this.ct三角矢印アニメ.n現在の値 / 50;
				int y = 289;

				// 描画。

				TJAPlayer3.Tx.SongSelect_Cursor_Left?.t2D描画(TJAPlayer3.app.Device, Cursor_L, y);
				TJAPlayer3.Tx.SongSelect_Cursor_Right?.t2D描画(TJAPlayer3.app.Device, Cursor_R, y);
			}
			//-----------------
			#endregion


			for (int i = 0; i < 13; i++)    // パネルは全13枚。
			{
				if ((i == 0 && this.n現在のスクロールカウンタ > 0) ||       // 最上行は、上に移動中なら表示しない。
					(i == 12 && this.n現在のスクロールカウンタ < 0))        // 最下行は、下に移動中なら表示しない。
					continue;

				int nパネル番号 = (((this.n現在の選択行 - 6) + i) + 13) % 13;

				if ((i == 6) && (this.n現在のスクロールカウンタ == 0) && (ctバー展開ディレイ用タイマー.b終了値に達した))
				{
					// (A) スクロールが停止しているときの選択曲バーの描画。

					#region [ タイトル名テクスチャを描画。]
					//-----------------
					if (this.stバー情報[nパネル番号].strタイトル文字列 != "" && this.ttk選択している曲の曲名 == null)
						this.ttk選択している曲の曲名 = this.ttk曲名テクスチャを生成する(this.stバー情報[nパネル番号].strタイトル文字列, Color.White, Color.Black);
					if (this.stバー情報[nパネル番号].strSubTitle != "" && this.ttk選択している曲のサブタイトル == null)
						this.ttk選択している曲のサブタイトル = this.ttkサブタイトルテクスチャを生成する(this.stバー情報[nパネル番号].strSubTitle);


					if (TJAPlayer3.Tx.SongSelect_Box_Center_Genre[nnGenreBack] != null && TJAPlayer3.Tx.SongSelect_Box_Center_Header_Genre[nnGenreBack] != null)
					{
						if (this.ttk選択している曲の曲名 != null)
						{
							if (!(this.r現在選択中の曲.eNodeType == C曲リストノード.ENodeType.BOX && TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.strGenre) != 0))
							{
								タイトルtmp = ResolveTitleTexture(this.ttk選択している曲の曲名);
								タイトルtmp.t2D描画(TJAPlayer3.app.Device, 750, TJAPlayer3.Skin.SongSelect_Overall_Y + TJAPlayer3.Skin.SongSelect_Box_Center_Header_Y_Diff + 23);
							}

						}
					}
					else if (this.ttk選択している曲の曲名 != null)
					{
						タイトルtmp = ResolveTitleTexture(this.ttk選択している曲の曲名);
						タイトルtmp.t2D描画(TJAPlayer3.app.Device, 750, TJAPlayer3.Skin.SongSelect_Overall_Y + TJAPlayer3.Skin.SongSelect_Box_Center_Header_Y_Diff + 23);
					}

					//サブタイトルがあったら700

					if (this.ttk選択している曲のサブタイトル != null)
					{
						サブタイトルtmp = ResolveTitleTexture(ttk選択している曲のサブタイトル);
						サブタイトルtmp.t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Down, 707 + (サブタイトルtmp.szTextureSize.Width / 2), TJAPlayer3.Skin.SongSelect_Overall_Y + 430);
					}

					//-----------------
					#endregion
				}

			}
			//-----------------

			if (this.r現在選択中の曲.eNodeType != C曲リストノード.ENodeType.BOX)
			{
				int genretextureindex = (this.r現在選択中の曲.r親ノード != null) ? TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.r親ノード.strGenre) : TJAPlayer3.Skin.nStrジャンルtoNum(this.r現在選択中の曲.strGenre);
				if (TJAPlayer3.Tx.SongSelect_GenreText != null)
				{
					TJAPlayer3.Tx.SongSelect_GenreText.t2D描画(TJAPlayer3.app.Device, CTexture.RefPnt.Center, BoxCenterx, TJAPlayer3.Skin.SongSelect_Overall_Y - 25, new Rectangle(0, genretextureindex * (TJAPlayer3.Tx.SongSelect_GenreText.szTextureSize.Height / (TJAPlayer3.Skin.MaxKeyNum + 2)), TJAPlayer3.Tx.SongSelect_GenreText.szTextureSize.Width, (TJAPlayer3.Tx.SongSelect_GenreText.szTextureSize.Height / (TJAPlayer3.Skin.MaxKeyNum + 2))));
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private struct STバー情報
		{
			public C曲リストノード.ENodeType eNodeType;
			public string strタイトル文字列;
			public int[] ar難易度;
			public bool[] b分岐;
			public Color ForeColor;
			public Color BackColor;
			public string strSubTitle;
			public TitleTextureKey ttkタイトル;
			public int[] n王冠;
			public C曲リストノード song;
		}

		private const int BoxCenterx = 645; 

		public bool b選択曲が変更された = true;
		private CCounter ct登場アニメ用;
		private CCounter ct三角矢印アニメ;
		private CCounter ct分岐フェード用タイマー;
		private CCounter ctバー展開用タイマー;
		private CCounter ctバー展開ディレイ用タイマー;
		private CCachedFontRenderer pfMusicName;
		private CCachedFontRenderer pfSubtitle;
		internal CTexture タイトルtmp;
		internal CTexture サブタイトルtmp;

		private readonly Dictionary<TitleTextureKey, CTexture> _titledictionary 
			= new Dictionary<TitleTextureKey, CTexture>();

		private long nスクロールタイマ;
		private int n現在のスクロールカウンタ;
		private int n現在の選択行;
		private int n目標のスクロールカウンタ;

		private Point[] ptバーの座標 = new Point[]
		{ new Point( -160, 180 ), new Point( -60, 180 ), new Point( 40, 180 ), new Point( 140, 180 ), new Point( 240, 180 ), new Point( 340, 180 ),
		  new Point( 590, 180 ),
		  new Point( 840, 180 ), new Point( 940, 180 ), new Point( 1040, 180 ), new Point( 1140, 180 ), new Point( 1240, 180 ), new Point( 1340, 180 ) };//2020.06.16 Mr-Ojii 諸事情により座標変更

		private STバー情報[] stバー情報 = new STバー情報[13];
		private CTexture txSongNotFound, txEnumeratingSongs;
		internal TitleTextureKey ttk選択している曲の曲名;
		internal TitleTextureKey ttk選択している曲のサブタイトル;

		private int nCurrentPosition = 0;
		private int nNumOfItems = 0;

		private C曲リストノード r次の曲(C曲リストノード song)
		{
			if (song == null)
				return null;

			
			List<C曲リストノード> list = (song.r親ノード != null && !TJAPlayer3.ConfigIni.OpenOneSide) ? song.r親ノード.list子リスト : TJAPlayer3.SongsManager.list曲ルート;

			int index = list.IndexOf(song);

			if (index < 0)
				return null;

			if (index == (list.Count - 1))
				return list[0];

			return list[index + 1];
		}
		private C曲リストノード r前の曲(C曲リストノード song)
		{
			if (song == null)
				return null;

			List<C曲リストノード> list = (song.r親ノード != null && !TJAPlayer3.ConfigIni.OpenOneSide) ? song.r親ノード.list子リスト : TJAPlayer3.SongsManager.list曲ルート;

			int index = list.IndexOf(song);

			if (index < 0)
				return null;

			if (index == 0)
				return list[list.Count - 1];

			return list[index - 1];
		}
		private void t現在選択中の曲を元に曲バーを再構成する()
		{
			C曲リストノード song = this.r現在選択中の曲;

			if (song == null)
				return;

			for (int i = 0; i < 6; i++)
				song = this.r前の曲(song);

			if (song == null)
			{
				if (TJAPlayer3.SongsManager.list曲ルート[0] != null)
				{
					this.r現在選択中の曲 = TJAPlayer3.SongsManager.list曲ルート[0];
					this.t現在選択中の曲を元に曲バーを再構成する();
					this.t選択曲が変更された(false);
					this.b選択曲が変更された = true;
					TJAPlayer3.stage選曲.t選択曲変更通知();
				}
				return;
			}

			for (int i = 0; i < 13; i++)
			{
				this.stバー情報[i].strタイトル文字列 = song.strTitle;
				this.stバー情報[i].eNodeType = song.eNodeType;
				this.stバー情報[i].strSubTitle = song.arスコア.譜面情報.strSubTitle;
				this.stバー情報[i].ar難易度 = song.arスコア.譜面情報.nレベル;
				this.stバー情報[i].b分岐 = song.arスコア.譜面情報.b譜面分岐;
				this.stバー情報[i].n王冠 = song.arスコア.譜面情報.n王冠;
				this.stバー情報[i].ForeColor = song.ForeColor;
				this.stバー情報[i].BackColor = song.BackColor;
				this.stバー情報[i].song = song;

				this.stバー情報[i].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[i].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor);

				song = this.r次の曲(song);
			}

			this.n現在の選択行 = 6;
		}
		private void tジャンル別選択されていない曲バーの描画(int x, int y, int nジャンル, C曲リストノード.ENodeType Eノード)
		{
			if (x >= GameWindowSize.Width || y >= GameWindowSize.Height)
				return;
			const int boxsabun = 10;

			if (Eノード == C曲リストノード.ENodeType.BACKBOX && TJAPlayer3.Tx.SongSelect_Bar_BackBox != null)
			{
				TJAPlayer3.Tx.SongSelect_Bar_BackBox.t2D描画(TJAPlayer3.app.Device, x, y);
			}
			else if (Eノード == C曲リストノード.ENodeType.BOX)
			{
				if (TJAPlayer3.Tx.SongSelect_Bar_Box_Genre[nジャンル] != null)
					TJAPlayer3.Tx.SongSelect_Bar_Box_Genre[nジャンル].t2D描画(TJAPlayer3.app.Device, x, y - boxsabun);
				else if (TJAPlayer3.Tx.SongSelect_Bar_Genre[nジャンル] != null)
					TJAPlayer3.Tx.SongSelect_Bar_Genre[nジャンル].t2D描画(TJAPlayer3.app.Device, x, y);
			}
			else
			{
				if (TJAPlayer3.Tx.SongSelect_Bar_Genre[nジャンル] != null)
					TJAPlayer3.Tx.SongSelect_Bar_Genre[nジャンル].t2D描画(TJAPlayer3.app.Device, x, y);
			}
		}

		private TitleTextureKey ttk曲名テクスチャを生成する(string str文字, Color forecolor, Color backcolor)
		{
			return new TitleTextureKey(str文字, pfMusicName, forecolor, backcolor, 410);
		}

		private TitleTextureKey ttkサブタイトルテクスチャを生成する(string str文字)
		{
			return new TitleTextureKey(str文字, pfSubtitle, Color.White, Color.Black, 390);
		}

		private CTexture ResolveTitleTexture(TitleTextureKey titleTextureKey)
		{
			if (!_titledictionary.TryGetValue(titleTextureKey, out var texture))
			{
				texture = GenerateTitleTexture(titleTextureKey);

				_titledictionary.Add(titleTextureKey, texture);
			}

			return texture;
		}

		private static CTexture GenerateTitleTexture(TitleTextureKey titleTextureKey)
		{
			using (var bmp = titleTextureKey.CCachedFontRenderer.DrawText_V(
				titleTextureKey.str文字, titleTextureKey.forecolor, titleTextureKey.backcolor, TJAPlayer3.Skin.Font_Edge_Ratio_Vertical))
			{
				CTexture tx文字テクスチャ = TJAPlayer3.tCreateTexture(bmp);
				if (tx文字テクスチャ.szTextureSize.Height > titleTextureKey.maxHeight)
				{
					tx文字テクスチャ.vcScaling.Y = (float)(((double)titleTextureKey.maxHeight) / tx文字テクスチャ.szTextureSize.Height);
				}

				return tx文字テクスチャ;
			}
		}

		private void ClearTitleTextureCache()
		{
			foreach (var titleTexture in _titledictionary.Values)
			{
				titleTexture.Dispose();
			}
			_titledictionary.Clear();
		}

		internal sealed class TitleTextureKey
		{
			public readonly string str文字;
			public readonly CCachedFontRenderer CCachedFontRenderer;
			public readonly Color forecolor;
			public readonly Color backcolor;
			public readonly int maxHeight;

			public TitleTextureKey(string str文字, CCachedFontRenderer CCachedFontRenderer, Color forecolor, Color backcolor, int maxHeight)
			{
				this.str文字 = str文字;
				this.CCachedFontRenderer = CCachedFontRenderer;
				this.forecolor = forecolor;
				this.backcolor = backcolor;
				this.maxHeight = maxHeight;
			}

			private bool Equals(TitleTextureKey other)
			{
				return string.Equals(str文字, other.str文字) &&
					   CCachedFontRenderer.Equals(other.CCachedFontRenderer) &&
					   forecolor.Equals(other.forecolor) &&
					   backcolor.Equals(other.backcolor) &&
					   maxHeight == other.maxHeight;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				return obj is TitleTextureKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = str文字.GetHashCode();
					hashCode = (hashCode * 397) ^ CCachedFontRenderer.GetHashCode();
					hashCode = (hashCode * 397) ^ forecolor.GetHashCode();
					hashCode = (hashCode * 397) ^ backcolor.GetHashCode();
					hashCode = (hashCode * 397) ^ maxHeight;
					return hashCode;
				}
			}

			public static bool operator ==(TitleTextureKey left, TitleTextureKey right)
			{
				return Equals(left, right);
			}

			public static bool operator !=(TitleTextureKey left, TitleTextureKey right)
			{
				return !Equals(left, right);
			}
		}

		internal void tアイテム数の描画()
		{
			if (TJAPlayer3.Tx.SongSelect_ItemNumber != null)
			{
				const int y = 560;
				const int 基準x = 1050;

				int x = 基準x;

				if (TJAPlayer3.Tx.SongSelect_ItemNumber_BG != null)
					TJAPlayer3.Tx.SongSelect_ItemNumber_BG.t2D描画(TJAPlayer3.app.Device, (TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Width / 11) + 基準x - TJAPlayer3.Tx.SongSelect_ItemNumber_BG.szTextureSize.Width, y - (TJAPlayer3.Tx.SongSelect_ItemNumber_BG.szTextureSize.Height - TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Height) / 2);

				string s = nNumOfItems.ToString();

				for (int p = s.Length - 1; p >= 0; p--)
				{
					tアイテム数の描画_１桁描画(x, y, s[p]);
					x -= (TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Width / 11);
				}

				tアイテム数の描画_１桁描画(基準x - 75, y, '/');

				x = 基準x - 100;

				s = nCurrentPosition.ToString();

				for (int p = s.Length - 1; p >= 0; p--)
				{
					tアイテム数の描画_１桁描画(x, y, s[p]);
					x -= (TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Width / 11);
				}
			}
		}
		private void tアイテム数の描画_１桁描画(int x, int y, char s数値)
		{
			if (TJAPlayer3.Tx.SongSelect_ItemNumber != null)
			{
				int dx;
				if (s数値 == '/')
				{
					dx = TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Width / 11 * 10;
				}
				else
				{
					int n = (int)s数値 - (int)'0';
					dx = (TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Width / 11) * n;
				}
				TJAPlayer3.Tx.SongSelect_ItemNumber.t2D描画(TJAPlayer3.app.Device, x, y, new Rectangle(dx, 0, (TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Width / 11), (TJAPlayer3.Tx.SongSelect_ItemNumber.szTextureSize.Height)));
			}
		}


		//数字フォント
		//-----------------
		#endregion
	}
}
