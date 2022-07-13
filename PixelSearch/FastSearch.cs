using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PixelSearch;

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
    /// <param name="clipX">X coordinate of the clip rect</param>
    /// <param name="clipY">Y coordinate of the clip rect</param>
    /// <param name="clipWidth">Width of the clip rect</param>
    /// <param name="clipHeight">Height of the clip rect</param>
    /// <param name="location">Location of the needle, if it was found; otherwise (-1, -1)</param>
    /// <returns><c>true</c> if the needle pixels were found from the haystack; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool FindPixels(IntPtr needle, int imgWidth, int imgHeight, IntPtr haystack, int rowLength, int clipX, int clipY, int clipWidth, int clipHeight, out (int x, int y) location) {

        if (clipWidth < imgWidth || clipHeight < imgHeight) {
            throw new ArgumentOutOfRangeException("clip width and height must be within image dimensions");
        }

        // calculate the last possible position of the clip
        // where the needle image can fit
        int endX = clipX + clipWidth - imgWidth;
        int endY = clipY + clipHeight - imgHeight;

        // also need to calculate the endX for vector range so we don't accidentally go over the range
        int vecEndX = endX - Vector<int>.Count;

        unsafe {

            // get the pointers to the img and haystack
            int* imgPtr = (int*)needle;
            int* haystackPtr = (int*)haystack;

            // create a scalar vector of the first pixel in the image
            Vector<int> firstPixelScalar = new Vector<int>(imgPtr[0]);

            for (int currentY = clipY; currentY < endY; currentY++) {

                int currentX = clipX;

                // first loop the vector possible range
                for (; currentX < vecEndX; currentX += Vector<int>.Count) {

                    // check if any pixels in the current position vector match the first pixel of the image

                    if (!Vector.EqualsAny(firstPixelScalar, *(Vector<int>*)(haystackPtr + currentX + currentY * rowLength))) {
                        continue;
                    }

                    // loop through the positions and check if every pixel matches with the image
                    for (int index = 0; index < Vector<int>.Count; index++) {
                        if (MatchAllPixels(imgPtr, haystackPtr + (currentX + index + currentY * rowLength), imgWidth, imgHeight, rowLength)) {
                            location = (currentX + index, currentY);
                            return true;
                        }
                    }
                }

                // the rest of the pixels in the row will be matched one by one
                for (; currentX < endX; currentX++) {
                    if (MatchAllPixels(imgPtr, haystackPtr + (currentX + currentY * rowLength), imgWidth, imgHeight, rowLength)) {
                        location = (currentX, currentY);
                        return true;
                    }
                }
            }

        }

        location = (-1, -1);

        return false;
    }

    /// <summary>
    /// Compares the needle pixels with the relative region inside the haystack
    /// </summary>
    /// <returns><c>true</c> if all pixels match; otherwise, <c>false</c></returns>
    unsafe private static bool MatchAllPixels(int* needlePtr, int* haystackRegion, int width, int height, int rowLength) {

        // loop through the pixels to find
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {

                // check if every pixel matches

                // we compare integer vs integer, so we dont have to check RGBA individually
                // also this way we ensure the comparison will work regardless of RGBA order
                if (needlePtr[x + y * width] != haystackRegion[x + y * rowLength]) {
                    return false;
                }
            }
        }

        return true;
    }
}
