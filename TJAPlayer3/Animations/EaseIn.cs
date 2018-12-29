﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3.Animations
{
    /// <summary>
    /// イーズインを行うクラス。
    /// </summary>
    class EaseIn : Animator
    {
        /// <summary>
        /// イーズインを初期化します。
        /// </summary>
        /// <param name="startPoint">始点。</param>
        /// <param name="endPoint">終点。</param>
        /// <param name="timeMs">イージングにかける時間。</param>
        public EaseIn(int startPoint, int endPoint, int timeMs) : base(0, timeMs, 1, false)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Sa = EndPoint - StartPoint;
            TimeMs = timeMs;
        }

        public override object GetAnimation()
        {
            var persent = (double)(base.Counter.n現在の値 / TimeMs);
            var progress = persent / TimeMs;
            Console.WriteLine(progress);
            if (base.Counter.b終了値に達した) base.Stop();
            return Sa * persent * persent * persent + StartPoint;
        }

        private readonly int StartPoint;
        private readonly int EndPoint;
        private readonly int Sa;
        private readonly int TimeMs;
    }
}
