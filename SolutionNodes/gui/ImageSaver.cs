using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using S = System.Windows.Size;

namespace SolutionNodes.gui {
	public class ImageSaver {

		/// <summary>Saves given element as png image into given stream using standard file dialog box.</summary>
		/// <param name="v">Element to be saved.</param>
		/// <param name="defaultName">Name for saved file. Do not provide extension (.png).</param>
		/// <param name="defaultLocation">Initial location of dialog box.</param>
		public static void asPNG(FrameworkElement v, string defaultName = null, string defaultLocation = null) {
			var sd = new SaveFileDialog() {
				FileName = (defaultName??"New Image") + ".png",
				InitialDirectory = defaultLocation,
				Filter = "PNG image file (*.png)|*.png|All files (*.*)|*.*"
			};
			if (sd.ShowDialog() != DialogResult.OK) return;
			var fs = File.Open(sd.FileName, FileMode.Create);
			asPNG(v, fs);
			fs.Close();
		}

		/// <summary>Saves given element as png image into given stream.</summary>
		/// <param name="v">Element to be saved.</param>
		/// <param name="output">Stream where image should be saved.</param>
		public static void asPNG(FrameworkElement v, Stream output)
			=> asPNG(getImage(v), output);

		/// <summary>Saves givnen bitma as png image into given stream.</summary>
		/// <param name="src">Bitmap to be saved.</param>
		/// <param name="outputStream">Stream where image should be saved.</param>
		public static void asPNG(BitmapSource src, Stream outputStream) {
			PngBitmapEncoder encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(src));
			encoder.Save(outputStream);
		}

		/// <summary>Returns bitmap from given visual.</summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public static RenderTargetBitmap getImage(FrameworkElement v) {
			S s = new S(v.ActualWidth, v.ActualHeight);
			if (s.IsEmpty) return null;

			var dv = new DrawingVisual();
			using (DrawingContext context = dv.RenderOpen()) {
				context.DrawRectangle(new VisualBrush(v),
					pen: null,
					rectangle: new Rect(s)
				);
				context.Close();
			}

			var dpi = getDPI(v);
			var b = new RenderTargetBitmap(
				(int)s.Width, (int)s.Height,
				dpi.x, dpi.y, PixelFormats.Pbgra32);
			b.Render(dv);
			return b;
		}

		/// <summary>Returns current DPI based on givne visual.</summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public static (double x, double y) getDPI(Visual v) {
			var src = PresentationSource.FromVisual(v);
			if (src == null) return (96, 96);
			return (
				96.0 * src.CompositionTarget.TransformToDevice.M11,
				96.0 * src.CompositionTarget.TransformToDevice.M22
			);
		}

	}
}
