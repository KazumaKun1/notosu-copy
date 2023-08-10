using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK;
using StorybrewCommon.Storyboarding;

namespace StorybrewScripts
{
    public class Vector2WithTimestamp
    {
        private object sliderEnd;
        private object value;

        public Vector2 Vector2 { get; }
        public double Timestamp { get; }
        public OsbSprite Sprite { get; }

        public Vector2WithTimestamp(Vector2 vector2, double timestamp, OsbSprite sprite)
        {
            Vector2 = vector2;
            Timestamp = timestamp;
            Sprite = sprite;
        }

        public Vector2WithTimestamp(object sliderEnd, object value)
        {
            this.sliderEnd = sliderEnd;
            this.value = value;
        }
    }
}