﻿using CSharpGL.Objects.Texts.FreeTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CSharpGL.Objects.Texts
{
    public static class TTFTextureYieldHelper
    {

        /// <summary>
        /// 用一个纹理绘制指定范围内的所有可见字符
        /// </summary>
        /// <param name="ttfFullname"></param>
        /// <param name="fontHeight">此值越大，绘制文字的清晰度越高，但占用的纹理资源就越多。</param>
        /// <param name="firstChar"></param>
        /// <param name="lastChar"></param>
        /// <param name="maxTextureWidth">生成的纹理的最大宽度。</param>
        /// <returns></returns>
        public static IEnumerable<TTFTextureYeildingState> GetTTFTexture(string ttfFullname, int fontHeight, char firstChar, char lastChar, int maxTextureWidth)
        {
            FreeTypeLibrary library = new FreeTypeLibrary();

            FreeTypeFace face = new FreeTypeFace(library, ttfFullname);

            Dictionary<char, CharacterInfo> charInfoDict = null;
            int textureWidth = 0, textureHeight = 0;
            System.Drawing.Bitmap bigBitmap = null;

            foreach (var item in GetTextureBlueprint(face, fontHeight, firstChar, lastChar, maxTextureWidth))
            {
                charInfoDict = item.dict;
                textureWidth = item.textureWidth;
                textureHeight = item.textureHeight;

                yield return item;
            }

            if (textureWidth == 0) { textureWidth = 1; }
            if (textureHeight == 0) { textureHeight = 1; }

            foreach (var item in GetBigBitmap(face, fontHeight, firstChar, lastChar, maxTextureWidth, charInfoDict, textureWidth, textureHeight))
            {
                bigBitmap = item.bigBitmap;

                yield return item;
            }

            face.Dispose();
            library.Dispose();

            var result = new TTFTexture()
            {
                TtfFullname = ttfFullname,
                FontHeight = fontHeight,
                FirstChar = firstChar,
                LastChar = lastChar,
                BigBitmap = bigBitmap,
                CharInfoDict = charInfoDict,
            };

            FileInfo fileInfo = new FileInfo(ttfFullname);
            yield return new TTFTextureYeildingState()
            {
                percent = 100,
                ttfTexture = result,
                message = string.Format("got ttf texture for {0}", fileInfo.Name),
            };
        }

        /// <summary>
        /// 根据<paramref name="charInfoDict"/>等信息把各个字形写入一个大的位图并返回之。
        /// </summary>
        /// <param name="face"></param>
        /// <param name="fontHeight"></param>
        /// <param name="firstChar"></param>
        /// <param name="lastChar"></param>
        /// <param name="maxTextureWidth"></param>
        /// <param name="charInfoDict"></param>
        /// <param name="widthOfTexture"></param>
        /// <param name="heightOfTexture"></param>
        /// <returns></returns>
        private static IEnumerable<TTFTextureYeildingState> GetBigBitmap(FreeTypeFace face, int fontHeight,
            char firstChar, char lastChar, int maxTextureWidth,
            Dictionary<char, CharacterInfo> charInfoDict,
            int widthOfTexture, int heightOfTexture)
        {
            int count = lastChar - firstChar;
            int index = 0;

            System.Drawing.Bitmap bigBitmap = new System.Drawing.Bitmap(widthOfTexture, heightOfTexture);
            Graphics graphics = Graphics.FromImage(bigBitmap);

            for (char c = firstChar; c <= lastChar; c++)
            {
                FreeTypeBitmapGlyph glyph = new FreeTypeBitmapGlyph(face, c, fontHeight);
                int size = glyph.obj.bitmap.width * glyph.obj.bitmap.rows;
                bool zeroBuffer = glyph.obj.bitmap.buffer == IntPtr.Zero;
                if ((size == 0) && (!zeroBuffer)) { throw new Exception(string.Format("glyph size({0}) for non zero buffer({1})", 0, glyph.obj.bitmap.buffer)); }
                if ((!(size == 0)) && zeroBuffer) { throw new Exception(string.Format("glyph size({0}) for zero buffer({1})", size, glyph.obj.bitmap.buffer)); }

                if (!(size == 0))
                {
                    byte[] byteBitmap = new byte[size];
                    Marshal.Copy(glyph.obj.bitmap.buffer, byteBitmap, 0, byteBitmap.Length);
                    CharacterInfo cInfo;
                    if (charInfoDict.TryGetValue(c, out cInfo))
                    {
                        if (cInfo.width > 0 && cInfo.height > 0)
                        {
                            System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
                            System.Drawing.Imaging.ImageLockMode lockMode = System.Drawing.Imaging.ImageLockMode.WriteOnly;
                            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(cInfo.width, cInfo.height, format);
                            Rectangle bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                            System.Drawing.Imaging.BitmapData bmpData = bitmap.LockBits(bitmapRect, lockMode, format);

                            int length = Math.Abs(bmpData.Stride) * bitmap.Height;
                            byte[] bitmapBytes = new byte[length];
                            for (int row = 0; row < cInfo.height; row++)
                            {
                                for (int col = 0; col < cInfo.width; col++)
                                {
                                    byte color = byteBitmap[row * cInfo.width + col];
                                    bitmapBytes[row * bmpData.Stride + col * 4 + 0] = color;
                                    bitmapBytes[row * bmpData.Stride + col * 4 + 1] = color;
                                    bitmapBytes[row * bmpData.Stride + col * 4 + 2] = color;
                                    //bitmapBytes[row * bmpData.Stride + col * 4 + 3] = color;
                                }
                            }

                            System.Runtime.InteropServices.Marshal.Copy(bitmapBytes, 0, bmpData.Scan0, length);

                            bitmap.UnlockBits(bmpData);

                            //int baseLine = fontHeight * 3 / 4 + 4;
                            //graphics.DrawImage(bitmap, cInfo.xoffset, cInfo.yoffset + baseLine - glyph.obj.top);
                            int skyHeight = fontHeight * 3 / 4 - glyph.obj.top;
                            if (skyHeight < 0) { skyHeight = 0; }
                            graphics.DrawImage(bitmap, cInfo.xoffset, cInfo.yoffset + skyHeight);
                        }
                    }
                    else
                    { throw new Exception(string.Format("Not support for display the char [{0}]", c)); }
                }

                if (c == char.MaxValue) { break; }

                yield return new TTFTextureYeildingState()
                {
                    percent = index++ * 100 / count,
                    message = string.Format("print glyph for {0}", c),
                };
            }

            graphics.Dispose();

            yield return new TTFTextureYeildingState()
            {
                percent = index++ * 100 / count,
                bigBitmap = bigBitmap,
                message = string.Format("texture is ready"),
            };
        }

        /// <summary>
        /// 根据<paramref name="firstChar"/>等信息获取要制作的贴图的宽高和各个字形的位置信息。
        /// </summary>
        /// <param name="face"></param>
        /// <param name="fontHeight"></param>
        /// <param name="firstChar"></param>
        /// <param name="lastChar"></param>
        /// <param name="maxTextureWidth"></param>
        /// <param name="charInfoDict"></param>
        /// <param name="textureWidth"></param>
        /// <param name="textureHeight"></param>
        private static IEnumerable<TTFTextureYeildingState> GetTextureBlueprint(FreeTypeFace face, int fontHeight,
            char firstChar, char lastChar, int maxTextureWidth)
        {
            int count = lastChar - firstChar;
            int index = 0;

            var charInfoDict = new Dictionary<char, CharacterInfo>();
            int textureWidth = 0;
            int textureHeight = fontHeight;

            int glyphXOffset = 0;
            int glyphYOffset = 0;

            for (char c = firstChar; c <= lastChar; c++)
            {
                FreeTypeBitmapGlyph glyph = new FreeTypeBitmapGlyph(face, c, fontHeight);

                bool zeroSize = (glyph.obj.bitmap.rows == 0 && glyph.obj.bitmap.width == 0);

                bool zeroBuffer = glyph.obj.bitmap.buffer == IntPtr.Zero;
                if (zeroSize && (!zeroBuffer)) { throw new Exception(); }
                if ((!zeroSize) && zeroBuffer) { throw new Exception(); }

                if (!zeroSize)
                {
                    int glyphWidth = glyph.obj.bitmap.width;
                    int glyphHeight = glyph.obj.bitmap.rows;

                    if (glyphXOffset + glyphWidth + 1 <= maxTextureWidth)
                    {
                        textureWidth = Math.Max(textureWidth, glyphXOffset + glyphWidth + 1);
                    }
                    else// 此字形将超出最大宽度，所以要换行。
                    {
                        textureHeight += fontHeight;

                        glyphXOffset = 0;
                        glyphYOffset = textureHeight - fontHeight;
                    }

                    CharacterInfo cInfo = new CharacterInfo();
                    cInfo.xoffset = glyphXOffset; cInfo.yoffset = glyphYOffset;
                    cInfo.width = glyphWidth; cInfo.height = glyphHeight;
                    charInfoDict.Add(c, cInfo);

                    glyphXOffset += glyphWidth + 1;
                }

                if (c == char.MaxValue) { break; }

                yield return new TTFTextureYeildingState()
                {
                    percent = index++ * 100 / count,
                    message = string.Format("generating blue print"),
                };
            }

            yield return new TTFTextureYeildingState()
            {
                percent = index++ * 100 / count,
                dict = charInfoDict,
                textureWidth = textureWidth,
                textureHeight = textureHeight,
                message = string.Format("generating blue print"),
            };
        }

        public class TTFTextureYeildingState
        {
            public string message;
            public int percent;
            public TTFTexture ttfTexture;
            internal System.Drawing.Bitmap bigBitmap;
            internal Dictionary<char, CharacterInfo> dict;
            internal int textureWidth;
            internal int textureHeight;
        }
    }
}
