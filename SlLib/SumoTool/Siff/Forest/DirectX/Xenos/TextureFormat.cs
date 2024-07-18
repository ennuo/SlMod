namespace SlLib.SumoTool.Siff.Forest.DirectX.Xenos;

public enum TextureFormat
{ 
  k_1_REVERSE = 0,
  k_1 = 1,
  k_8 = 2,
  k_1_5_5_5 = 3,
  k_5_6_5 = 4,
  k_6_5_5 = 5,
  k_8_8_8_8 = 6,
  k_2_10_10_10 = 7,
  // Possibly similar to k_8, but may be storing alpha instead of red when
  // resolving/memexporting, though not exactly known. From the point of view of
  // sampling, it should be treated the same as k_8 (given that textures have
  // the last - and single-component textures have the only - component
  // replicated into all the remaining ones before the swizzle).
  // Used as:
  // - Texture in 4B4E083C - text, starting from the "Loading..." and the "This
  //   game saves data automatically" messages. The swizzle in the fetch
  //   constant is 111W (suggesting that internally the only component may be
  //   the alpha one, not red).
  // TODO(Triang3l): Investigate how k_8_A and k_8_B work in resolves and
  // memexports, whether they store alpha/blue of the input or red.
  k_8_A = 8,
  k_8_B = 9,
  k_8_8 = 10,
  // Though it's unknown what exactly REP means, likely it's "repeating
  // fraction" (the term used for normalized fixed-point formats, UNORM in
  // particular for unsigned signedness - 0.0 to 1.0 range, like in
  // Direct3D 10+, unlike the 0.0 to 255.0 range for D3DFMT_R8G8_B8G8 and
  // D3DFMT_G8R8_G8B8 in Direct3D 9). 54540829 uses k_Y1_Cr_Y0_Cb_REP directly
  // as UNORM.
  k_Cr_Y1_Cb_Y0_REP = 11,
  // Used for videos in 54540829.
  k_Y1_Cr_Y0_Cb_REP = 12,
  k_16_16_EDRAM = 13,
  // Likely same as k_8_8_8_8.
  // Used as:
  // - Memexport destination in 4D5308BC - multiple small draws when looking
  //   back at the door behind the player in the first room of gameplay.
  // - Memexport destination in 4D53085B and 4D530919 - in 4D53085B, in a frame
  //   between the intro video and the main menu, in a 8192-point draw.
  k_8_8_8_8_A = 14,
  k_4_4_4_4 = 15,
  k_10_11_11 = 16,
  k_11_11_10 = 17,
  k_DXT1 = 18,
  k_DXT2_3 = 19,
  k_DXT4_5 = 20,
  k_16_16_16_16_EDRAM = 21,
  k_24_8 = 22,
  k_24_8_FLOAT = 23,
  k_16 = 24,
  k_16_16 = 25,
  k_16_16_16_16 = 26,
  k_16_EXPAND = 27,
  k_16_16_EXPAND = 28,
  k_16_16_16_16_EXPAND = 29,
  k_16_FLOAT = 30,
  k_16_16_FLOAT = 31,
  k_16_16_16_16_FLOAT = 32,
  k_32 = 33,
  k_32_32 = 34,
  k_32_32_32_32 = 35,
  k_32_FLOAT = 36,
  k_32_32_FLOAT = 37,
  k_32_32_32_32_FLOAT = 38,
  k_32_AS_8 = 39,
  k_32_AS_8_8 = 40,
  k_16_MPEG = 41,
  k_16_16_MPEG = 42,
  k_8_INTERLACED = 43,
  k_32_AS_8_INTERLACED = 44,
  k_32_AS_8_8_INTERLACED = 45,
  k_16_INTERLACED = 46,
  k_16_MPEG_INTERLACED = 47,
  k_16_16_MPEG_INTERLACED = 48,
  k_DXN = 49,
  k_8_8_8_8_AS_16_16_16_16 = 50,
  k_DXT1_AS_16_16_16_16 = 51,
  k_DXT2_3_AS_16_16_16_16 = 52,
  k_DXT4_5_AS_16_16_16_16 = 53,
  k_2_10_10_10_AS_16_16_16_16 = 54,
  k_10_11_11_AS_16_16_16_16 = 55,
  k_11_11_10_AS_16_16_16_16 = 56,
  k_32_32_32_FLOAT = 57,
  k_DXT3A = 58,
  k_DXT5A = 59,
  k_CTX1 = 60,
  k_DXT3A_AS_1_1_1_1 = 61,
  k_8_8_8_8_GAMMA_EDRAM = 62,
  k_2_10_10_10_FLOAT_EDRAM = 63,
}