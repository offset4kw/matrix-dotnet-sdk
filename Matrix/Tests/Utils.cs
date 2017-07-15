using System;
using Matrix.Structures;

namespace Matrix.Tests
{
    public class Utils
    {
        public static MatrixEvent MockEvent(MatrixEventContent content)
        {
            MatrixEvent ev = new MatrixEvent();
            ev.content = content;
            return ev;
        }
    }
}
