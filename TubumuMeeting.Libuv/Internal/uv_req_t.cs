using System;
using System.Runtime.InteropServices;

namespace TubumuMeeting.Libuv
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct uv_req_t
    {
        public IntPtr data;
        public RequestType type;
    }
}
