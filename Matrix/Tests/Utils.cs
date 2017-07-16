using System;
using Matrix.Structures;

namespace Matrix.Tests
{
    public class Utils
    {
        public static MatrixEvent MockEvent(
            MatrixEventContent content,
            string state_key = null,
            int age = 0)
        {
            MatrixEvent ev = new MatrixEvent();
            ev.content = content;
            if(state_key != null) {
                ev.state_key = state_key;
            }
            ev.age = age;
            return ev;
        }
    }
}
