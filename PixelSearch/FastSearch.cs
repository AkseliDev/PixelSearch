using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PixelSearch;

/// <summary>
/// A class that provides a set of functions for pixel searching
/// </summary>
public static class FastSearch {

    /// <summary>
    /// Compares pixels of an image with pixels in the haystack (ex. the screen pixels). 
    /// The pixels must be in a 32 bit format, but the color itself can be in any RGBA order. 
    /// 
    /// <para>The needle and the haystack should be in row-major order for the method to work correctly.</para>
    /// </summary>
    /// <param name="needle">Pointer to the beginning of the image pixels to be searched</param>
    /// <param name="imgWidth">The needle image width</param>
    /// <param name="imgHeight">The needle image height</param>
    /// <param name="haystack">Pointer to the begging of the haystack pixels to be searched</param>
    /// <param name="haystackWidth">Width of the haystack</param>
    /// <param name="clipX">X coordinate of the clip rect</param>
    /// <param name="clipY">Y coordinate of the clip rect</param>
    /// <param name="clipWidth">Width of the clip rect</param>
    /// <param name="clipHeight">Height of the clip rect</param>
    /// <param name="location">Location of the needle, if it was found; otherwise (-1, -1)</param>
    /// <returns><c>true</c> if the needle pixels were found from the haystack; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool FindPixels(IntPtr needle, int imgWidth, int imgHeight, IntPtr haystack, int haystackWidth, int clipX, int clipY, int clipWidth, int clipHeight, out (int x, int y) location) {
        unsafe {
            return FindPixels(ref ((int*)needle)[0], imgWidth, imgHeight, ref ((int*)haystack)[0], haystackWidth, clipX, clipY, clipWidth, clipHeight, out location);
        }
    }

    /// <summary>
    /// Compares pixels of an image with pixels in the haystack (ex. the screen pixels). 
    /// The pixels must be in a 32 bit format, but the color itself can be in any RGBA order. 
    /// 
    /// <para>The needle and the haystack should be in row-major order for the method to work correctly.</para>
    /// </summary>
    /// <param name="needle">Span of the image pixels to be searched</param>
    /// <param name="imgWidth">The needle image width</param>
    /// <param name="imgHeight">The needle image height</param>
    /// <param name="haystack">Span of the haystack pixels to be searched</param>
    /// <param name="haystackWidth">Width of the haystack</param>
    /// <param name="clipX">X coordinate of the clip rect</param>
    /// <param name="clipY">Y coordinate of the clip rect</param>
    /// <param name="clipWidth">Width of the clip rect</param>
    /// <param name="clipHeight">Height of the clip rect</param>
    /// <param name="location">Location of the needle, if it was found; otherwise (-1, -1)</param>
    /// <returns><c>true</c> if the needle pixels were found from the haystack; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool FindPixels(Span<int> needle, int imgWidth, int imgHeight, Span<int> haystack, int haystackWidth, int clipX, int clipY, int clipWidth, int clipHeight, out (int x, int y) location) {
        if (needle.Length < imgWidth * imgHeight) {
            throw new ArgumentOutOfRangeException(nameof(needle));
        }
        if (haystack.Length < clipWidth * clipHeight) {
            throw new ArgumentOutOfRangeException(nameof(haystack));
        }
        return FindPixels(ref needle[0], imgWidth, imgHeight, ref haystack[0], haystackWidth, clipX, clipY, clipWidth, clipHeight, out location);
    }

    /// <summary>
    /// Compares pixels of an image with pixels in the haystack (ex. the screen pixels). 
    /// The pixels must be in a 32 bit format, but the color itself can be in any RGBA order. 
    /// 
    /// <para>The needle and the haystack should be in row-major order for the method to work correctly.</para>
    /// </summary>
    /// <param name="needle">Reference to the beginning of the image pixels to be searched</param>
    /// <param name="imgWidth">The needle image width</param>
    /// <param name="imgHeight">The needle image height</param>
    /// <param name="haystack">Reference to the begging of the haystack pixels to be searched</param>
    /// <param name="haystackWidth">Width of the haystack</param>
    /// <param name="clipX">X coordinate of the clip rect</param>
    /// <param name="clipY">Y coordinate of the clip rect</param>
    /// <param name="clipWidth">Width of the clip rect</param>
    /// <param name="clipHeight">Height of the clip rect</param>
    /// <param name="location">Location of the needle, if it was found; otherwise (-1, -1)</param>
    /// <returns><c>true</c> if the needle pixels were found from the haystack; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool FindPixels(ref int needle, int imgWidth, int imgHeight, ref int haystack, int haystackWidth, int clipX, int clipY, int clipWidth, int clipHeight, out (int x, int y) location) {

        if (clipWidth < imgWidth || clipHeight < imgHeight) {
            throw new ArgumentOutOfRangeException("clip", "clip width and height must be within image dimensions");
        }

        // calculate the last possible position of the clip
        // where the needle image can fit
        int endX = clipX + clipWidth - imgWidth;
        int endY = clipY + clipHeight - imgHeight;

        // also need to calculate the endX for vector range so we don't accidentally go over the range
        int vecEndX = endX - Vector<int>.Count;

        // create a scalar vector of the first pixel in the image for comparison
        Vector<int> firstPixelScalar = new Vector<int>(needle);

        for (int currentY = clipY; currentY < endY; currentY++) {

            int currentX = clipX;

            // first loop the possible vector range
            for (; currentX < vecEndX; currentX += Vector<int>.Count) {

                // check if any pixels in the current position vector match the first pixel of the image
                Vector<int> searchVector = Unsafe.ReadUnaligned<Vector<int>>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref haystack, currentX + currentY * haystackWidth)));
                if (!Vector.EqualsAny(firstPixelScalar, searchVector)) {
                    continue;
                }

                // loop through the positions and check if every pixel matches with the image
                ref int searchRegion = ref Unsafe.Add(ref haystack, currentX + currentY * haystackWidth);
                for (int index = 0; index < Vector<int>.Count; index++) {
                    if (MatchAllPixels(ref needle, ref searchRegion, imgWidth, imgHeight, haystackWidth)) {
                        location = (currentX + index, currentY);
                        return true;
                    }
                    searchRegion = ref Unsafe.Add(ref searchRegion, 1);
                }
            }

            // the rest of the pixels in the row will be matched one by one
            for (; currentX < endX; currentX++) {
                if (MatchAllPixels(ref needle, ref Unsafe.Add(ref haystack, currentX + currentY * haystackWidth), imgWidth, imgHeight, haystackWidth)) {
                    location = (currentX, currentY);
                    return true;
                }
            }
        }

        location = (-1, -1);

        return false;
    }

    /// <summary>
    /// Searches through a region in the haystack and tries to match it with the needle pixels
    /// </summary>
    /// <param name="needlePtr">Beginning of the needle image pixels</param>
    /// <param name="haystackRegion">Beginning of the region to be searched</param>
    /// <param name="width">Width of the needle pixels</param>
    /// <param name="height">Height of the needle pixels</param>
    /// <param name="rowLength">Length of one row in the haystack</param>
    /// <returns><c>true</c> if all pixels matched</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchAllPixels(ref int needlePtr, ref int haystackRegion, int width, int height, int rowLength) {

        if (width < Vector<int>.Count || rowLength < Vector<int>.Count) {
            // loop through the pixels to find
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {

                    // check if every pixel matches

                    // we compare integer vs integer, so we dont have to check RGBA individually
                    // also this way we ensure the comparison will work regardless of RGBA order
                    int a = Unsafe.ReadUnaligned<int>(ref Unsafe.As<int, byte>(ref needlePtr));
                    int b = Unsafe.ReadUnaligned<int>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref haystackRegion, x + y * rowLength)));
                    if (a != b) {
                        return false;
                    }
                    needlePtr = ref Unsafe.Add(ref needlePtr, 1);
                }
            }
        } else {
            for (int y = 0; y < height; y++) {
                int x = 0;

                for (; x < width - Vector<int>.Count; x += Vector<int>.Count) {

                    Vector<int> a = Unsafe.ReadUnaligned<Vector<int>>(ref Unsafe.As<int,byte>(ref needlePtr));
                    Vector<int> b = Unsafe.ReadUnaligned<Vector<int>>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref haystackRegion, x + y * rowLength)));
                    
                    // we can do comparison on Vector<int>.Count elements at the same time with vectors
                    if (a != b) {
                        return false;
                    }
                    
                    needlePtr = ref Unsafe.Add(ref needlePtr, Vector<int>.Count);
                }
                for (; x < width; x++) {

                    // we still need to do scalar comparison for the rest of the pixels
                    int a = Unsafe.ReadUnaligned<int>(ref Unsafe.As<int, byte>(ref needlePtr));
                    int b = Unsafe.ReadUnaligned<int>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref haystackRegion, x + y * rowLength)));
                    if (a != b) {
                        return false;
                    }
                    needlePtr = ref Unsafe.Add(ref needlePtr, 1);
                }
            }
        }

        return true;
    }
}