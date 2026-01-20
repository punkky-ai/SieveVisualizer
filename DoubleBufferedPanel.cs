using System.Windows.Forms;

namespace SieveVisualizer
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        // ป้องกันการ "ลบพื้นหลัง" ที่ทำให้กระพริบ
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // ไม่ต้องเรียก base เพื่อลด flicker
            // base.OnPaintBackground(e);
        }
    }
}
