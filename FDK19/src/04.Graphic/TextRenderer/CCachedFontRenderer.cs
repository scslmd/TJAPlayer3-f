﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Color = System.Drawing.Color;

namespace FDK
{
	/// <summary>
	/// 高速描画版のCFontRendererクラス。
	/// といっても、一度レンダリングした結果をキャッシュして使いまわしているだけ。
	/// </summary>
	public class CCachedFontRenderer : CFontRenderer
	{
		#region [ コンストラクタ ]
		public CCachedFontRenderer( string fontpath, int pt, SixLabors.Fonts.FontStyle style )
		{
			Initialize( fontpath, pt, style );
		}
		public CCachedFontRenderer( string fontpath, int pt )
		{
			Initialize( fontpath, pt, SixLabors.Fonts.FontStyle.Regular );
		}
		public CCachedFontRenderer()
		{
			throw new ArgumentException("CCachedFontRenderer: 引数があるコンストラクタを使用してください。");
		}
		#endregion
		#region [ コンストラクタから呼ばれる初期化処理 ]
		protected new void Initialize( string fontpath, int pt, SixLabors.Fonts.FontStyle style )
		{
			this.bDisposed_CCachedFontRenderer = false;
			this.listFontCache = new List<FontCache>();
			base.Initialize( fontpath, pt, style );
		}
		#endregion


		#region [ DrawTextのオーバーロード群 ]
		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <returns>描画済テクスチャ</returns>
		public new Image<Rgba32> DrawText( string drawstr, Color fontColor )
		{
			return DrawText( drawstr, DrawMode.Normal, fontColor, Color.White, Color.White, Color.White, 0 );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public new Image<Rgba32> DrawText( string drawstr, Color fontColor, Color edgeColor, int edge_Ratio)
		{
			return DrawText( drawstr, DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White, edge_Ratio );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public Image<Rgba32> DrawText( string drawstr, Color fontColor, Color edgeColor, DrawMode dMode, int edge_Ratio)
		{
			return DrawText( drawstr, dMode, fontColor, edgeColor, Color.White, Color.White, edge_Ratio );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public new Image<Rgba32> DrawText( string drawstr, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio )
		{
			return DrawText( drawstr, DrawMode.Edge | DrawMode.Gradation, fontColor, edgeColor, gradationTopColor, gradataionBottomColor, edge_Ratio );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public new Image<Rgba32> DrawText_V( string drawstr, Color fontColor, Color edgeColor, int edge_Ratio )
		{
			return DrawText_V(drawstr, DrawMode.Edge, fontColor, edgeColor, Color.Black, Color.Black, edge_Ratio);
		}

		#endregion

		protected new Image<Rgba32> DrawText( string drawstr, DrawMode drawmode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio )
		{
			#region [ 以前レンダリングしたことのある文字列/フォントか? (キャッシュにヒットするか?) ]
			int index = listFontCache.FindIndex(
				delegate( FontCache fontcache )
				{
					return (
						drawstr == fontcache.drawstr &&
						drawmode == fontcache.drawmode &&
						fontColor == fontcache.fontColor &&
						edgeColor == fontcache.edgeColor &&
						gradationTopColor == fontcache.gradationTopColor &&
						gradationBottomColor == fontcache.gradationBottomColor &&
						fontcache.Vertical == false
					);
				}
			);
			#endregion
			if ( index < 0 )
			{
				// キャッシュにヒットせず。
				#region [ レンダリングして、キャッシュに登録 ]
				FontCache fc = new FontCache();
				fc.bmp = base.DrawText( drawstr, drawmode, fontColor, edgeColor, gradationTopColor, gradationBottomColor, edge_Ratio);
				fc.drawstr = drawstr;
				fc.drawmode = drawmode;
				fc.fontColor = fontColor;
				fc.edgeColor = edgeColor;
				fc.gradationTopColor = gradationTopColor;
				fc.gradationBottomColor = gradationBottomColor;
				fc.Vertical = false;
				listFontCache.Add( fc );
				Debug.WriteLine( drawstr + ": Cacheにヒットせず。(cachesize=" + listFontCache.Count + ")" );
				#endregion
				#region [ もしキャッシュがあふれたら、最も古いキャッシュを破棄する ]
				if ( listFontCache.Count > MAXCACHESIZE )
				{
					Debug.WriteLine( "Cache溢れ。" + listFontCache[ 0 ].drawstr + " を解放します。" );
					if ( listFontCache[ 0 ].bmp != null )
					{
						listFontCache[ 0 ].bmp.Dispose();
					}
					listFontCache.RemoveAt( 0 );
				}
				#endregion

				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ listFontCache.Count - 1 ].bmp.Clone();
			}
			else
			{
				Debug.WriteLine( drawstr + ": Cacheにヒット!! index=" + index );
				#region [ キャッシュにヒット。レンダリングは行わず、キャッシュ内のデータを返して終了。]
				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ index ].bmp.Clone();
				#endregion
			}
		}

		protected new Image<Rgba32> DrawText_V(string drawstr, DrawMode drawmode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio)
		{
			#region [ 以前レンダリングしたことのある文字列/フォントか? (キャッシュにヒットするか?) ]
			int index = listFontCache.FindIndex(
				delegate (FontCache fontcache)
				{
					return (
						drawstr == fontcache.drawstr &&
						drawmode == fontcache.drawmode &&
						fontColor == fontcache.fontColor &&
						edgeColor == fontcache.edgeColor &&
						gradationTopColor == fontcache.gradationTopColor &&
						gradationBottomColor == fontcache.gradationBottomColor &&
						fontcache.Vertical == true
					);
				}
			);
			#endregion
			if ( index < 0 )
			{
				// キャッシュにヒットせず。
				#region [ レンダリングして、キャッシュに登録 ]
				FontCache fc = new FontCache();
				fc.bmp = base.DrawText_V(drawstr, drawmode, fontColor, edgeColor, gradationTopColor, gradationBottomColor, edge_Ratio);
				fc.drawstr = drawstr;
				fc.fontColor = fontColor;
				fc.edgeColor = edgeColor;
				fc.gradationTopColor = gradationTopColor;
				fc.gradationBottomColor = gradationBottomColor;
				fc.Vertical = true;
				listFontCache.Add( fc );
				Debug.WriteLine( drawstr + ": Cacheにヒットせず。(cachesize=" + listFontCache.Count + ")" );
				#endregion
				#region [ もしキャッシュがあふれたら、最も古いキャッシュを破棄する ]
				if ( listFontCache.Count > MAXCACHESIZE )
				{
					Debug.WriteLine( "Cache溢れ。" + listFontCache[ 0 ].drawstr + " を解放します。" );
					if ( listFontCache[ 0 ].bmp != null )
					{
						listFontCache[ 0 ].bmp.Dispose();
					}
					listFontCache.RemoveAt( 0 );
				}
				#endregion

				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ listFontCache.Count - 1 ].bmp.Clone();
			}
			else
			{
				Debug.WriteLine( drawstr + ": Cacheにヒット!! index=" + index );
				#region [ キャッシュにヒット。レンダリングは行わず、キャッシュ内のデータを返して終了。]
				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ index ].bmp.Clone();
				#endregion
			}
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public new void Dispose()
		{
			if (!this.bDisposed_CCachedFontRenderer)
			{
				if (listFontCache != null)
				{
					//Debug.WriteLine( "Disposing CCachedFontRenderer()" );
					#region [ キャッシュしている画像を破棄する ]
					foreach (FontCache bc in listFontCache)
					{
						if (bc.bmp != null)
						{
							bc.bmp.Dispose();
						}
					}
					#endregion
					listFontCache.Clear();
					listFontCache = null;
				}
				this.bDisposed_CCachedFontRenderer = true;
			}
			base.Dispose();
		}
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		/// <summary>
		/// キャッシュ容量
		/// </summary>
		private const int MAXCACHESIZE = 256;

		private struct FontCache
		{
			// public Font font;
			public string drawstr;
			public DrawMode drawmode;
			public Color fontColor;
			public Color edgeColor;
			public Color gradationTopColor;
			public Color gradationBottomColor;
			public Image<Rgba32> bmp;
			public bool Vertical;
		}
		private List<FontCache> listFontCache;

		protected bool bDisposed_CCachedFontRenderer;
		//-----------------
		#endregion
	}
}
