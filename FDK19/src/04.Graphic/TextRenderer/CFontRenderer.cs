using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;

using Color = System.Drawing.Color;

namespace FDK
{

	public class CFontRenderer : IDisposable
	{
		#region[static系]
		public static void SetTextCorrectionX_Chara_List_Vertical(string[] list)
		{
			if (list != null)
				CorrectionX_Chara_List_Vertical = list.Where(c => c != null).ToArray();
		}
		public static void SetTextCorrectionX_Chara_List_Value_Vertical(int[] list)
		{
			if (list != null)
				CorrectionX_Chara_List_Value_Vertical = list;
		}
		public static void SetTextCorrectionY_Chara_List_Vertical(string[] list)
		{
			if (list != null)
				CorrectionY_Chara_List_Vertical = list.Where(c => c != null).ToArray();
		}
		public static void SetTextCorrectionY_Chara_List_Value_Vertical(int[] list)
		{
			if (list != null)
				CorrectionY_Chara_List_Value_Vertical = list;
		}
		public static void SetRotate_Chara_List_Vertical(string[] list)
		{
			if (list != null)
				Rotate_Chara_List_Vertical = list.Where(c => c != null).ToArray();
		}

		private static string[] CorrectionX_Chara_List_Vertical = new string[0];
		private static int[] CorrectionX_Chara_List_Value_Vertical = new int[0];
		private static string[] CorrectionY_Chara_List_Vertical = new string[0];
		private static int[] CorrectionY_Chara_List_Value_Vertical = new int[0];
		private static string[] Rotate_Chara_List_Vertical = new string[0];
		#endregion



		[Flags]
		public enum DrawMode
		{
			Normal,
			Edge,
			Gradation,
			Vertical
		}

		public static string DefaultFontName
		{
			get
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					return "MS UI Gothic";
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					return "ヒラギノ角ゴ Std W8";//OSX搭載PC未所持のため暫定
				else
					return "Droid Sans Fallback";
			}
		}

		#region [ コンストラクタ ]
		public CFontRenderer(string fontpath, int pt, SixLabors.Fonts.FontStyle style)
		{
			Initialize(fontpath, pt, style);
		}
		public CFontRenderer(string fontpath, int pt)
		{
			Initialize(fontpath, pt, SixLabors.Fonts.FontStyle.Regular);
		}
		public CFontRenderer()
		{
			//throw new ArgumentException("CFontRenderer: 引数があるコンストラクタを使用してください。");
		}
		#endregion

		protected void Initialize(string fontpath, int pt, FontStyle style)
		{
			try
			{
				this.textRenderer = new CSixLaborsTextRenderer(fontpath, pt, style);
				return;
			}
			catch (Exception e)
			{
				Trace.TraceWarning("SixLabors.Fontsでのフォント生成に失敗しました。" + e.ToString());
				this.textRenderer?.Dispose();
			}

			try
			{
				this.textRenderer = new CGDIPlusTextRenderer(fontpath, pt, style);
				return;
			}
			catch (Exception e)
			{
				Trace.TraceWarning("GDI+でのフォント生成に失敗しました。" + e.ToString());
				this.textRenderer?.Dispose();
			}

			try 
			{
				this.textRenderer = new CSixLaborsTextRenderer(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"FDK.mplus-1p-medium.ttf"), pt, style);
			}
			catch (Exception e)
			{
				Trace.TraceWarning("ビルトインフォントを使用してのフォント生成に失敗しました。" + e.ToString());
				this.textRenderer?.Dispose();
				throw;
			}
		}

		public Image<Rgba32> DrawText(string drawstr, Color fontColor)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Normal, fontColor, Color.White, Color.White, Color.White, 0);
		}

		public Image<Rgba32> DrawText(string drawstr, Color fontColor, Color edgeColor, int edge_Ratio)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White, edge_Ratio);
		}

		public Image<Rgba32> DrawText(string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Gradation, fontColor, Color.White, gradationTopColor, gradataionBottomColor, edge_Ratio);
		}

		public Image<Rgba32> DrawText(string drawstr, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Edge | CFontRenderer.DrawMode.Gradation, fontColor, edgeColor, gradationTopColor, gradataionBottomColor, edge_Ratio);
		}
		protected Image<Rgba32> DrawText(string drawstr, CFontRenderer.DrawMode drawmode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio)
		{
			//横書きに対してのCorrectionは廃止
			return this.textRenderer.DrawText(drawstr, drawmode, fontColor, edgeColor, gradationTopColor, gradationBottomColor, edge_Ratio);
		}


		public Image<Rgba32> DrawText_V(string drawstr, Color fontColor)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Normal, fontColor, Color.White, Color.White, Color.White, 0);
		}

		public Image<Rgba32> DrawText_V(string drawstr, Color fontColor, Color edgeColor, int edge_Ratio)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White, edge_Ratio);
		}

		public Image<Rgba32> DrawText_V(string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Gradation, fontColor, Color.White, gradationTopColor, gradataionBottomColor, edge_Ratio);
		}

		public Image<Rgba32> DrawText_V(string drawstr, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Edge | CFontRenderer.DrawMode.Gradation, fontColor, edgeColor, gradationTopColor, gradataionBottomColor, edge_Ratio);
		}
		protected Image<Rgba32> DrawText_V(string drawstr, CFontRenderer.DrawMode drawmode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio)
		{
			if (string.IsNullOrEmpty(drawstr))
			{
				//nullか""だったら、1x1を返す
				return new Image<Rgba32>(1, 1);
			}

			//グラデ(全体)にも対応したいですね？

			string[] strList = new string[drawstr.Length];
			for (int i = 0; i < drawstr.Length; i++)
				strList[i] = drawstr.Substring(i, 1);
			Image<Rgba32>[] strImageList = new Image<Rgba32>[drawstr.Length];

			//レンダリング,大きさ計測
			int nWidth = 0;
			int nHeight = 0;
			for (int i = 0; i < strImageList.Length; i++)
			{
				strImageList[i] = this.textRenderer.DrawText(strList[i], drawmode, fontColor, edgeColor, gradationTopColor, gradationBottomColor, edge_Ratio);

				//回転する文字
				if(Rotate_Chara_List_Vertical.Contains(strList[i]))
					strImageList[i].Mutate(ctx => ctx.Rotate(RotateMode.Rotate90));

				nWidth = Math.Max(nWidth, strImageList[i].Width);
				nHeight += strImageList[i].Height;
			}

			Image<Rgba32> image = new Image<Rgba32>(nWidth, nHeight);

			//1文字ずつ描画したやつを全体キャンバスに描画していく
			int nowHeightPos = 0;
			for (int i = 0; i < strImageList.Length; i++)
			{
				int Correction_X = 0, Correction_Y = 0;
				if (CorrectionX_Chara_List_Vertical != null && CorrectionX_Chara_List_Value_Vertical != null)
				{
					int Xindex = Array.IndexOf(CorrectionX_Chara_List_Vertical, strList[i]);
					if (-1 < Xindex && Xindex < CorrectionX_Chara_List_Value_Vertical.Length && CorrectionX_Chara_List_Vertical.Contains(strList[i]))
					{
						Correction_X = CorrectionX_Chara_List_Value_Vertical[Xindex];
					}
					else
					{
						if (-1 < Xindex && CorrectionX_Chara_List_Value_Vertical.Length <= Xindex && CorrectionX_Chara_List_Vertical.Contains(strList[i]))
						{
							Correction_X = CorrectionX_Chara_List_Value_Vertical[0];
						}
						else
						{
							Correction_X = 0;
						}
					}
				}

				if (CorrectionY_Chara_List_Vertical != null && CorrectionY_Chara_List_Value_Vertical != null)
				{
					int Yindex = Array.IndexOf(CorrectionY_Chara_List_Vertical, strList[i]);
					if (-1 < Yindex && Yindex < CorrectionY_Chara_List_Value_Vertical.Length && CorrectionY_Chara_List_Vertical.Contains(strList[i]))
					{
						Correction_Y = CorrectionY_Chara_List_Value_Vertical[Yindex];
					}
					else
					{
						if (-1 < Yindex && CorrectionY_Chara_List_Value_Vertical.Length <= Yindex && CorrectionY_Chara_List_Vertical.Contains(strList[i]))
						{
							Correction_Y = CorrectionY_Chara_List_Value_Vertical[0];
						}
						else
						{
							Correction_Y = 0;
						}
					}
				}

				image.Mutate(ctx => ctx.DrawImage(strImageList[i], new Point((nWidth - strImageList[i].Width) / 2 + Correction_X, nowHeightPos + Correction_Y), 1));
				nowHeightPos += strImageList[i].Height;
			}

			//1文字ずつ描画したやつの解放
			for (int i = 0; i < strImageList.Length; i++)
			{
				strImageList[i].Dispose();
			}

			//返します
			return image;
		}

		public void Dispose()
		{
			this.textRenderer.Dispose();
		}

		private ITextRenderer textRenderer;
	}
}