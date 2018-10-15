using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dots3
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [MTAThread]
        static void Main(string[] args)
        {
            Application.Run(new DeviceProvisor(args));
        }
    }
}