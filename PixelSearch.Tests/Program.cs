#if WINDOWS
using System.Drawing;
using System.Drawing.Imaging;
#endif
using PixelSearch;

// the bitmap api is only available on windows

#if WINDOWS
static void Test_FindImageOnScreen() {

    const string ImageFile = "./resources/Capture.PNG";

    const int ScreenWidth = 1920;
    const int ScreenHeight = 1080;


    // first get the image to search
    using var imageBmp = new Bitmap(ImageFile);

    // then get the screen pixels
    // for that, generate an empty bitmap with the screen dimensions
    using var screenBmp = new Bitmap(ScreenWidth, ScreenHeight);
    
    // then create a graphics context from the bitmap
    using var graphics = Graphics.FromImage(screenBmp);

    // then copy data from the screen over into the generated bitmap
    graphics.CopyFromScreen(0, 0, 0, 0, new Size(ScreenWidth, ScreenHeight));


    // to access the raw pixel data of the bitmaps, they need to be locked
    var imgData = imageBmp.LockBits(new Rectangle(Point.Empty, imageBmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    var screenData = screenBmp.LockBits(new Rectangle(Point.Empty, screenBmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


    // now that we have access to the pixel data, we can finally perform the pixel search

    if (FastSearch.FindPixels(
            needle: imgData.Scan0, // the needle is the pixel data of the image to be searched
            imgWidth: imageBmp.Width,
            imgHeight: imageBmp.Height,
            
            haystack: screenData.Scan0, // the haystack is the pixel data where the image will be searched from, in this case the screen
            haystackWidth: screenBmp.Width, // we also need to input the haystack width

            // clip rect is the area where the image will be searched from inside the screen
            clipX: 0,
            clipY: 0,
            clipWidth: ScreenWidth,
            clipHeight: ScreenHeight,

            out var location // finally the location where the image was found
    )) {
        Console.WriteLine($"Image was found at (x: {location.x}, y: {location.y})");
    } else {
        Console.WriteLine("The image could not be found from the screen.");
    }


    // its important to unlock the memory after the operation
    imageBmp.UnlockBits(imgData);
    screenBmp.UnlockBits(screenData);
}

Test_FindImageOnScreen();
#else
Console.WriteLine("This test is only available on Windows");
#endif