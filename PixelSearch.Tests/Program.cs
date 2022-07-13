using System.Drawing;
using System.Drawing.Imaging;
using PixelSearch;

// the bitmap api is only available on windows

#if WINDOWS
static void Test_FindImageOnScreen() {

    const string ImageFile = "Capture.PNG";

}


Test_FindImageOnScreen();
#endif