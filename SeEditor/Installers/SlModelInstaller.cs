using OpenTK.Graphics.OpenGL;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Model;

namespace SeEditor.Installers;

public static class SlModelInstaller
{
    public static void Install(SlModel model)
    {
        model.Convert(SlPlatform.Win32);
        if (model.Resource.Segments.Count == 0) return;

        foreach (SlStream stream in model.Resource.PlatformResource.VertexStreams)
        {
            if (stream.VBO != 0) continue;
            stream.VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, stream.VBO);

            byte[] t = new byte[stream.Data.Count];
            stream.Data.CopyTo(t);
            GL.BufferData(BufferTarget.ArrayBuffer, t.Length, t, BufferUsageHint.StaticDraw);
        }

        var indexStream = model.Resource.PlatformResource.IndexStream;
        if (indexStream.VBO == 0)
        {
            indexStream.VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexStream.VBO);

            byte[] t = new byte[indexStream.Data.Count];
            indexStream.Data.CopyTo(t);
            GL.BufferData(BufferTarget.ElementArrayBuffer, t.Length, t, BufferUsageHint.StaticDraw);
        }

        foreach (SlModelSegment segment in model.Resource.Segments)
        {
            if (segment.VAO != 0) continue;
            segment.VAO = GL.GenVertexArray();

            GL.BindVertexArray(segment.VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, segment.IndexStream.VBO);
            foreach (SlVertexAttribute attribute in segment.Format.GetFlattenedAttributes())
            {
                var stream = segment.VertexStreams[attribute.Stream]!;

                GL.BindBuffer(BufferTarget.ArrayBuffer, stream.VBO);

                var type = VertexAttribPointerType.Float;

                bool normalized = false;
                switch (attribute.Type)
                {
                    case SlVertexElementType.Float:
                        type = VertexAttribPointerType.Float;
                        break;
                    case SlVertexElementType.Half:
                        type = VertexAttribPointerType.HalfFloat;
                        break;
                    case SlVertexElementType.UByte:
                        type = VertexAttribPointerType.UnsignedByte;
                        break;
                    case SlVertexElementType.UByteN:
                        type = VertexAttribPointerType.UnsignedByte;
                        normalized = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                GL.EnableVertexAttribArray(attribute.Usage);
                GL.VertexAttribPointer(attribute.Usage, attribute.Count, type, normalized, stream.Stride,
                    (segment.VertexStart * stream.Stride) + attribute.Offset);
            }

            // if (segment.WeightBuffer.Count != 0)
            // {
            //     GL.EnableVertexAttribArray(SlVertexUsage.BlendIndices);
            //     
            //     
            //     GL.EnableVertexAttribArray(SlVertexUsage.BlendWeight);
            //     
            //     
            //     
            //     
            // }


            GL.BindVertexArray(0);
        }
    }
}