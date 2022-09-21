using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Все типы используют тип double
/// </summary>
namespace Core.LowLevel
{
    [Serializable]
    public struct RangeValue
    {
        //der was been deleted
        private double min, max, val, ovf;

        /// <summary>
        /// Минимальное значение
        /// </summary>
        public double MinValue
        {
            get => min;
            set
            {
                if(value>max)
                {
                    min = max;
                    max = value;
                }
                else
                {
                    min = value;
                }

                if (val < min)
                    val = min;
            }
        }
        /// <summary>
        /// Максимальное значение
        /// </summary>
        public double MaxValue
        {
            get => max;
            set
            {
                if (value < max)
                {
                    max = min;
                    min = value;
                }
                else
                {
                    max = value;
                }

                if (val > max)
                    val = max;
            }
        }

        /// <summary>
        /// Max + overflow
        /// </summary>
        public double OverflowValue
        {
            get => max + ovf;
            set
            {
                var delta = value - max;
                if (delta <= 0)
                {
                    ovf = 0;
                    return;
                }

                ovf = delta;
            }
        }
        /// <summary>
        /// Текущее значение. Limit max + overflow
        /// </summary>
        public double Value
        {
            get => val;
            set
            {
                if (value < min)
                    val = min;
                else if (value > OverflowValue)
                    val = OverflowValue;
                else val = value;
            }
        }
        /// <summary>
        /// Max value - value
        /// </summary>
        public double InvertedValue
        {
            get => MaxValue - Value;
        }
        /// <summary>
        /// Limit max
        /// </summary>
        public double SafeValue
        {
            get => val;
            set
            {
                if (value < min)
                    val = min;
                else if (value > MaxValue)
                    val = MaxValue;
                else val = value;
            }
        }
        /*utils*/
        /// <summary>
        /// Max - min
        /// </summary>
        public double Range => max - min;
        /// <summary>
        /// Max + overflow - min
        /// </summary>
        public double FullRange => OverflowValue - min;
        /// <summary>
        /// On default range
        /// </summary>
        public double Percentage
        {
            get
            {
                var rn = max - min;
                if (double.IsNaN(rn) || rn is 0) return 0;
                return (val - min) / rn;
            }

            set
            {
                Value = Range * value;
            }
        }
        /// <summary>
        /// val <= min
        /// </summary>
        public bool IsMin => val <= min;
        /// <summary>
        /// val >= max
        /// </summary>
        public bool IsMax => val >= max;
        /// <summary>
        /// val > max
        /// </summary>
        public bool IsOverflow => val > max;
        /// <summary>
        /// val == max + overflow
        /// </summary>
        public bool IsFull => val >= OverflowValue;
        /// <summary>
        /// val + add >= max
        /// </summary>
        /// <param name="additionalValue"></param>
        /// <returns></returns>
        public bool TryOverflow(double additionalValue)
        {
            return (val + additionalValue) >= max;
        }
        
        public RangeValue(double minVal, double maxVal, double value)
        {
            min = 0; max = maxVal; val = 0; ovf = 0;

            MinValue = minVal;
            Value = value;
        }
        public RangeValue(double minVal, double maxVal, double overflowVal, double val)
        {
            min = minVal; max = maxVal; this.val = 0; ovf = overflowVal;

            Value = val;
        }
        public RangeValue(double maxVal, double val) : this(0, maxVal, val) { }
        
    }
}

