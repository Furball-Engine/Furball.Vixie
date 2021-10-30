using System.Drawing;
using Silk.NET.OpenGL;

namespace Furball.Vixie.Gl {
    public class Texture {
        private GL  gl;
        private int _textureId;



        public Texture(string filepath) {
            this.gl = Global.Gl;
        }

        public Texture(Bitmap bitmap) {
            this.gl = Global.Gl;
        }

        private void Load(Bitmap bitmap) {
            Color[][] matrix;

            int height = bitmap.Height;
            int width = bitmap.Width;

            if (height > width)
            {
                matrix = new Color[bitmap.Width][];
                for (int i = 0; i <= bitmap.Width - 1; i++)
                {
                    matrix[i] = new Color[bitmap.Height];
                    for (int j = 0; j < bitmap.Height - 1; j++)
                    {
                        matrix[i][j] = bitmap.GetPixel(i, j);
                    }
                }
            }
            else
            {
                matrix = new Color[bitmap.Height][];
                for (int i = 0; i <= bitmap.Height - 1; i++)
                {
                    matrix[i] = new Color[bitmap.Width];
                    for (int j = 0; j < bitmap.Width - 1; j++)
                    {
                        matrix[i][j] = bitmap.GetPixel(i, j);
                    }
                }
            }
        }


        public void Bind() {

        }

        public void Unbind() {

        }
    }
}
