﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SDL2;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace FDK
{
	public class CTexture : IDisposable
	{
		// プロパティ
		public EBlendMode eBlendMode = EBlendMode.Normal;

		public float fRotation;
		public int Opacity
		{
			get
			{
				return this._opacity;
			}
			set
			{
				if (value < 0)
				{
					this._opacity = 0;
				}
				else if (value > 0xff)
				{
					this._opacity = 0xff;
				}
				else
				{
					this._opacity = value;
				}
			}
		}
		public Size szTextureSize
		{
			get;
			private set;
		}
		private IntPtr? texture;
		public Vector3 vcScaling;
		private string filename;

		// コンストラクタ

		public CTexture()
		{
			this.szTextureSize = new Size(0, 0);
			this._opacity = 0xff;
			this.texture = null;
			this.bTextureDisposed = true;
			this.fRotation = 0f;
			this.vcScaling = new Vector3(1f, 1f, 1f);
			this.filename = "";
		}

		/// <summary>
		/// <para>指定された画像ファイルから Managed テクスチャを作成する。</para>
		/// <para>利用可能な画像形式は、BMP, JPG, PNG, TGA, DDS, PPM, DIB, HDR, PFM のいずれか。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="strFilename">画像ファイル名。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTexture(Device device, string strFilename)
			: this()
		{
			maketype = MakeType.filename;
			filename = strFilename;
			MakeTexture(device, strFilename);
		}
		public CTexture(Device device, Image<Rgba32> image, bool b黒を透過する)
			: this()
		{
			maketype = MakeType.bitmap;
			MakeTexture(device, image, b黒を透過する);
		}

		public void MakeTexture(Device device, string strFilename)
		{
			if (!File.Exists(strFilename))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
				throw new FileNotFoundException(string.Format("File does not exist. \n[{0}]", strFilename));

			using (SixLabors.ImageSharp.Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(strFilename))
				MakeTexture(device, image, false);
		}
		public void MakeTexture(Device device, SixLabors.ImageSharp.Image<Rgba32> bitmap, bool b黒を透過する)
		{
			if (b黒を透過する)
				bitmap.Mutate(c => c.BackgroundColor(SixLabors.ImageSharp.Color.Transparent));
			try
			{
				this.szTextureSize = new Size(bitmap.Width, bitmap.Height);
				this.rcImageRect = new Rectangle(0, 0, this.szTextureSize.Width, this.szTextureSize.Height);

				this.texture = SDL.SDL_CreateTexture(device.renderer, SDL.SDL_PIXELFORMAT_ABGR8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, szTextureSize.Width, szTextureSize.Height);
				
				SDL.SDL_Rect rect = new SDL.SDL_Rect()
				{
					x = 0,
					y = 0,
					w = bitmap.Width,
					h = bitmap.Height
				};
				if(bitmap.TryGetSinglePixelSpan(out Span<Rgba32> span))
                {
                    unsafe
                    {
						fixed(Rgba32* ptr = span)
                        {
							SDL.SDL_UpdateTexture((IntPtr)this.texture, ref rect, (IntPtr)ptr, bitmap.Width * 4);
						}
                    }
                }
                else
                {
					throw new CTextureCreateFailedException("GetPixelData failed");
				}

				SDL.SDL_SetTextureBlendMode((IntPtr)this.texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

				this.bTextureDisposed = false;
			}
			catch
			{
				this.Dispose();
				throw new CTextureCreateFailedException(string.Format("Failed to create texture. \n"));
			}
		}
		// メソッド
		public void UpdateTexture(IntPtr bitmap, Size size) 
		{
			if (this.szTextureSize == size)
			{
				SDL.SDL_Rect rect = new SDL.SDL_Rect()
				{
					x = 0,
					y = 0,
					w = size.Width,
					h = size.Height
				};
				SDL.SDL_UpdateTexture((IntPtr)this.texture, ref rect, bitmap, size.Width * 4);
			}
		}

		public void t2D描画(Device device, RefPnt refpnt, float x, float y) 
		{
			this.t2D描画(device, refpnt, x, y, rcImageRect);
		}
		public void t2D描画(Device device, RefPnt refpnt, float x, float y, Rectangle rect)
		{
			this.t2D描画(device, refpnt, x, y, 1f, rect);
		}
		public void t2D描画(Device device, RefPnt refpnt, float x, float y, float depth, Rectangle rect)
		{
			switch (refpnt)
			{
				case RefPnt.UpLeft:
					this.t2D描画(device, x, y, depth, rect);
					break;
				case RefPnt.Up:
					this.t2D描画(device, x - (rect.Width / 2), y, depth, rect);
					break;
				case RefPnt.UpRight:
					this.t2D描画(device, x - rect.Width, y, depth, rect);
					break;
				case RefPnt.Left:
					this.t2D描画(device, x, y - (rect.Height / 2), depth, rect);
					break;
				case RefPnt.Center:
					this.t2D描画(device, x - (rect.Width / 2), y - (rect.Height / 2), depth, rect);
					break;
				case RefPnt.Right:
					this.t2D描画(device, x - rect.Width, y - (rect.Height / 2), depth, rect);
					break;
				case RefPnt.DownLeft:
					this.t2D描画(device, x, y - rect.Height, depth, rect);
					break;
				case RefPnt.Down:
					this.t2D描画(device, x - (rect.Width / 2), y - rect.Height, depth, rect);
					break;
				case RefPnt.DownRight:
					this.t2D描画(device, x - rect.Width, y - rect.Height, depth, rect);
					break;
				default:
					break;
			}
		}

		public void t2D拡大率考慮描画(Device device, RefPnt refpnt, float x, float y)
		{
			this.t2D拡大率考慮描画(device, refpnt, x, y, rcImageRect);
		}
		public void t2D拡大率考慮描画(Device device, RefPnt refpnt, float x, float y, Rectangle rect)
		{
			this.t2D拡大率考慮描画(device, refpnt, x, y, 1f, rect);
		}
		public void t2D拡大率考慮描画(Device device, RefPnt refpnt, float x, float y, float depth, Rectangle rect)
		{
			switch (refpnt)
			{
				case RefPnt.UpLeft:
					this.t2D描画(device, x, y, depth, rect);
					break;
				case RefPnt.Up:
					this.t2D描画(device, x - (rect.Width / 2 * this.vcScaling.X), y, depth, rect);
					break;
				case RefPnt.UpRight:
					this.t2D描画(device, x - rect.Width * this.vcScaling.X, y, depth, rect);
					break;
				case RefPnt.Left:
					this.t2D描画(device, x, y - (rect.Height / 2 * this.vcScaling.Y), depth, rect);
					break;
				case RefPnt.Center:
					this.t2D描画(device, x - (rect.Width / 2 * this.vcScaling.X), y - (rect.Height / 2 * this.vcScaling.Y), depth, rect);
					break;
				case RefPnt.Right:
					this.t2D描画(device, x - rect.Width * this.vcScaling.X, y - (rect.Height / 2 * this.vcScaling.Y), depth, rect);
					break;
				case RefPnt.DownLeft:
					this.t2D描画(device, x, y - rect.Height * this.vcScaling.Y, depth, rect);
					break;
				case RefPnt.Down:
					this.t2D描画(device, x - (rect.Width / 2 * this.vcScaling.X), y - rect.Height * this.vcScaling.Y, depth, rect);
					break;
				case RefPnt.DownRight:
					this.t2D描画(device, x - rect.Width * this.vcScaling.X, y - rect.Height * this.vcScaling.Y, depth, rect);
					break;
				default:
					break;
			}
		}
		public void t2D元サイズ基準描画(Device device, RefPnt refpnt, float x, float y)
		{
			this.t2D元サイズ基準描画(device, refpnt, x, y, rcImageRect);
		}
		public void t2D元サイズ基準描画(Device device, RefPnt refpnt, float x, float y, Rectangle rect)
		{
			this.t2D元サイズ基準描画(device, refpnt, x, y, 1f, rect);
		}
		public void t2D元サイズ基準描画(Device device, RefPnt refpnt, float x, float y, float depth, Rectangle rect)
		{
			switch (refpnt)
			{
				case RefPnt.UpLeft:
					this.t2D描画(device, x, y, depth, rect);
					break;
				case RefPnt.Up:
					this.t2D描画(device, x - (szTextureSize.Width / 2), y, depth, rect);
					break;
				case RefPnt.UpRight:
					this.t2D描画(device, x - szTextureSize.Width, y, depth, rect);
					break;
				case RefPnt.Left:
					this.t2D描画(device, x, y - (szTextureSize.Height / 2), depth, rect);
					break;
				case RefPnt.Center:
					this.t2D描画(device, x - (szTextureSize.Width / 2), y - (szTextureSize.Height / 2), depth, rect);
					break;
				case RefPnt.Right:
					this.t2D描画(device, x - szTextureSize.Width, y - (szTextureSize.Height / 2), depth, rect);
					break;
				case RefPnt.DownLeft:
					this.t2D描画(device, x, y - szTextureSize.Height, depth, rect);
					break;
				case RefPnt.Down:
					this.t2D描画(device, x - (szTextureSize.Width / 2), y - szTextureSize.Height, depth, rect);
					break;
				case RefPnt.DownRight:
					this.t2D描画(device, x - szTextureSize.Width, y - szTextureSize.Height, depth, rect);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// テクスチャを 2D 画像と見なして描画する。
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
		/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
		public void t2D描画(Device device, float x, float y)
		{
			this.t2D描画(device, x, y, 1f, this.rcImageRect);
		}
		public void t2D描画(Device device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x, y, 1f, rc画像内の描画領域);
		}
		public void t2D描画(Device device, float x, float y, float depth, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				return;

			this.tSetBlendMode(device);

			SDL.SDL_SetTextureAlphaMod((IntPtr)this.texture, (byte)this._opacity);
			SDL.SDL_SetTextureColorMod((IntPtr)this.texture, (byte)this.color.R, (byte)this.color.G, (byte)this.color.B);


			dstrect.x = (int)x;
			dstrect.y = (int)y;
			dstrect.w = (int)(rc画像内の描画領域.Width * this.vcScaling.X);
			dstrect.h = (int)(rc画像内の描画領域.Height * this.vcScaling.Y);

			srcrect.x = rc画像内の描画領域.X;
			srcrect.y = rc画像内の描画領域.Y;
			srcrect.w = rc画像内の描画領域.Width;
			srcrect.h = rc画像内の描画領域.Height;

			SDL.SDL_RenderCopyEx(device.renderer, (IntPtr)this.texture, ref srcrect, ref dstrect, -(this.fRotation * 180 / Math.PI), IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
		}


		public void t2D幕用描画(Device device, float x, float y, Rectangle rc画像内の描画領域, bool left, int num = 0) 
		{
			if (this.texture == null)
				return;

			this.tSetBlendMode(device);

			SDL.SDL_SetTextureAlphaMod((IntPtr)this.texture, (byte)this._opacity);
			SDL.SDL_SetTextureColorMod((IntPtr)this.texture, (byte)this.color.R, (byte)this.color.G, (byte)this.color.B);

			dstrect.x = (int)x;
			dstrect.y = (int)y;
			dstrect.w = (int)(rc画像内の描画領域.Width * this.vcScaling.X);
			dstrect.h = (int)(rc画像内の描画領域.Height * this.vcScaling.Y);

			srcrect.x = rc画像内の描画領域.X;
			srcrect.y = rc画像内の描画領域.Y;
			srcrect.w = rc画像内の描画領域.Width;
			srcrect.h = rc画像内の描画領域.Height;

			SDL.SDL_RenderCopy(device.renderer, (IntPtr)this.texture, ref srcrect, ref dstrect);
		}

		public void t2D上下反転描画(Device device, float x, float y)
		{
			this.t2D上下反転描画(device, x, y, 1f, this.rcImageRect);
		}
		public void t2D上下反転描画(Device device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D上下反転描画(device, x, y, 1f, rc画像内の描画領域);
		}
		public void t2D上下反転描画(Device device, float x, float y, float depth, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				throw new InvalidOperationException("Texture is not generated. ");

			this.tSetBlendMode(device);

			SDL.SDL_SetTextureAlphaMod((IntPtr)this.texture, (byte)this._opacity);
			SDL.SDL_SetTextureColorMod((IntPtr)this.texture, (byte)this.color.R, (byte)this.color.G, (byte)this.color.B);

			dstrect.x = (int)x;
			dstrect.y = (int)y;
			dstrect.w = (int)(rc画像内の描画領域.Width * this.vcScaling.X);
			dstrect.h = (int)(rc画像内の描画領域.Height * this.vcScaling.Y);

			srcrect.x = rc画像内の描画領域.X;
			srcrect.y = rc画像内の描画領域.Y;
			srcrect.w = rc画像内の描画領域.Width;
			srcrect.h = rc画像内の描画領域.Height;

			SDL.SDL_RenderCopyEx(device.renderer, (IntPtr)this.texture, ref srcrect, ref dstrect, 0, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_VERTICAL);
		}

		public void t2D左右反転描画(Device device, float x, float y)
		{
			this.t2D左右反転描画(device, x, y, 1f, this.rcImageRect);
		}
		public void t2D左右反転描画(Device device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D左右反転描画(device, x, y, 1f, rc画像内の描画領域);
		}
		public void t2D左右反転描画(Device device, float x, float y, float depth, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				throw new InvalidOperationException("Texture is not generated. ");

			this.tSetBlendMode(device);

			SDL.SDL_SetTextureAlphaMod((IntPtr)this.texture, (byte)this._opacity);
			SDL.SDL_SetTextureColorMod((IntPtr)this.texture, (byte)this.color.R, (byte)this.color.G, (byte)this.color.B);

			dstrect.x = (int)x;
			dstrect.y = (int)y;
			dstrect.w = (int)(rc画像内の描画領域.Width * this.vcScaling.X);
			dstrect.h = (int)(rc画像内の描画領域.Height * this.vcScaling.Y);

			srcrect.x = rc画像内の描画領域.X;
			srcrect.y = rc画像内の描画領域.Y;
			srcrect.w = rc画像内の描画領域.Width;
			srcrect.h = rc画像内の描画領域.Height;

			SDL.SDL_RenderCopyEx(device.renderer, (IntPtr)this.texture, ref srcrect, ref dstrect, 0, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL);
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if (!this.bDisposed)
			{
				// テクスチャの破棄
				if (this.texture.HasValue)
				{
					this.bTextureDisposed = true;
					if(this.texture != null)
						SDL.SDL_DestroyTexture((IntPtr)this.texture);
					this.texture = null;
				}

				this.bDisposed = true;
			}
		}
		~CTexture()
		{
			// ファイナライザの動作時にtextureのDisposeがされていない場合は、
			// CTextureのDispose漏れと見做して警告をログ出力する
			if (!this.bTextureDisposed)//DTXManiaより
			{
				Trace.TraceWarning("CTexture: Texture memory leak detected.(Size=({0}, {1}), filename={2}, maketype={3})", szTextureSize.Width, szTextureSize.Height, filename, maketype.ToString());
			}
		}
		//-----------------
		#endregion

		// その他

		public enum RefPnt
		{
			UpLeft,
			Up,
			UpRight,
			Left,
			Center,
			Right,
			DownLeft,
			Down,
			DownRight,
		}

		public enum EBlendMode 
		{
			Normal,
			Addition,
		}

		#region [ private ]
		//-----------------
		private int _opacity;
		private bool bDisposed, bTextureDisposed;

		/// <summary>
		/// どれか一つが有効になります。
		/// </summary>
		/// <param name="device">Direct3Dのデバイス</param>
		private void tSetBlendMode(Device device)
		{
			switch (this.eBlendMode) 
			{
				case EBlendMode.Addition:
					SDL.SDL_SetTextureBlendMode((IntPtr)this.texture, SDL.SDL_BlendMode.SDL_BLENDMODE_ADD);
					break;
				default:
					SDL.SDL_SetTextureBlendMode((IntPtr)this.texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
					break;
			}
		}

		private enum MakeType
		{
			filename,
			bytearray,
			bitmap
		}

		// 2012.3.21 さらなる new の省略作戦

		protected Rectangle rcImageRect;                              // テクスチャ作ったらあとは不変
		public Color color = Color.FromArgb(255, 255, 255, 255);
		private MakeType maketype = MakeType.bytearray;
		private SDL.SDL_Rect srcrect;
		private SDL.SDL_Rect dstrect;
		//-----------------
		#endregion
	}
}
