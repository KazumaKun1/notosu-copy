using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewScripts
{

    public class NoteOrigin
    {

        public string receptorSpritePath = "";
        public StoryboardLayer layer;
        public OsbSprite originSprite;

        public SortedDictionary<double, float> positionX = new SortedDictionary<double, float>();
        public SortedDictionary<double, float> positionY = new SortedDictionary<double, float>();

        public double bpmOffset;
        public double bpm;

        public OsbSprite debug;

        // Rotation in radiants
        public double rotation = 0f;

        private double deltaIncrement = 1;

        public NoteOrigin(String receptorSpritePath, double rotation, StoryboardLayer layer, CommandScale scale, double starttime, double delta)
        {

            this.deltaIncrement = delta;

            OsbSprite origin = layer.CreateSprite("sb/transparent.png", OsbOrigin.Centre);
            origin.Rotate(starttime - 1, rotation);
            origin.ScaleVec(starttime - 1, scale);

            positionX.Add(0, 0);
            positionY.Add(0, 0);

            this.receptorSpritePath = receptorSpritePath;
            this.rotation = rotation;
            this.layer = layer;
            this.originSprite = origin;

        }

        public void Render(double starttime, double endTime)
        {
            OsbSprite receptor = this.originSprite;

            receptor.Fade(starttime, 1);
            receptor.Fade(endTime, 0);

        }

        public void MoveOriginAbsolute(double starttime, Vector2 endPos)
        {

            AddXValue(starttime, endPos.X, endPos.X, true);
            AddYValue(starttime, endPos.Y, endPos.Y, true);


        }

        public void MoveOriginAbsolute(OsbEasing ease, double starttime, double endtime, Vector2 startPos, Vector2 endPos)
        {

            if (starttime == endtime)
            {
                AddXValue(starttime, endPos.X, endPos.X, true);
                AddYValue(starttime, endPos.Y, endPos.Y, true);
                return;
            }

            easeProgressAbsolute(ease, starttime, endtime, startPos, endPos);

        }

        public void MoveOriginRelative(OsbEasing ease, double starttime, double endtime, Vector2 offset)
        {

            if (starttime == endtime)
            {
                AddXValue(starttime, offset.X, offset.X);
                AddYValue(starttime, offset.Y, offset.Y);
                return;
            }

            easeProgressRelative(ease, starttime, endtime, offset);

        }

        public void MoveOriginRelative(OsbEasing ease, double starttime, double endtime, Vector2 offset, Vector2 absolute)
        {

            if (starttime == endtime)
            {
                AddXValue(starttime, offset.X, absolute.X);
                AddYValue(starttime, offset.Y, absolute.Y);
                return;
            }

            easeProgressRelative(ease, starttime, endtime, offset);

        }

        public void MoveOriginRelativeX(OsbEasing ease, double starttime, double endtime, float value)
        {

            if (starttime == endtime)
            {
                AddXValue(starttime, value, value);
                return;
            }

            easeProgressRelative(ease, starttime, endtime, new Vector2(value, 0));

        }

        public void MoveOriginRelativeY(OsbEasing ease, double starttime, double endtime, float value)
        {
            if (starttime == endtime)
            {
                AddYValue(starttime, value, value);
                return;
            }

            easeProgressRelative(ease, starttime, endtime, new Vector2(0, value));

        }

        public void ScaleReceptor(OsbEasing ease, double starttime, double endtime, Vector2 newScale)
        {
            OsbSprite receptor = this.originSprite;

            if (starttime == endtime)
            {
                receptor.ScaleVec(starttime, newScale);
            }
            else
            {
                receptor.ScaleVec(ease, starttime, endtime, ScaleAt(starttime), newScale);
            }

        }

        public void RotateReceptor(OsbEasing ease, double starttime, double endtime, double rotation)
        {
            OsbSprite receptor = this.originSprite;

            var newRotation = this.rotation + rotation;

            if (starttime == endtime)
            {
                receptor.Rotate(starttime, newRotation);
            }
            else
            {
                receptor.Rotate(ease, starttime, endtime, RotationAt(starttime), newRotation);
            }

            this.rotation = newRotation;

        }

        public void PivotOrigin(OsbEasing ease, double starttime, double endtime, double rotation, Vector2 center)
        {
            Vector2 point = PositionAt(starttime);

            double duration = Math.Max(endtime - starttime, 1);
            double endRadians = rotation; // Total rotation in radians

            Vector2 currentPosition = point;
            double currentTime = starttime;

            while (currentTime <= endtime)
            {
                currentTime += deltaIncrement;
                double progress = Math.Max(currentTime - starttime, 1) / duration; // Calculate progress as a ratio

                // Adjust the rotation based on progress and easing
                double easedProgress = ease.Ease(progress); // Assuming ease.Ease() applies the easing to the progress
                double currentRotation = endRadians * easedProgress; // Total rotation adjusted by eased progress

                Vector2 rotatedPoint = Utility.PivotPoint(point, center, currentRotation);

                Vector2 relativeMovement = rotatedPoint - currentPosition;
                Vector2 absoluteMovement = rotatedPoint - point;

                MoveOriginRelative(ease, currentTime, currentTime, relativeMovement, absoluteMovement);

                currentPosition = rotatedPoint;
            }
        }

        public static Vector2 PivotPoint(Vector2 point, Vector2 center, double radians)
        {
            // Translate point back to origin
            point -= center;

            // Rotate point
            Vector2 rotatedPoint = new Vector2(
                point.X * (float)Math.Cos(radians) - point.Y * (float)Math.Sin(radians),
                point.X * (float)Math.Sin(radians) + point.Y * (float)Math.Cos(radians)
            );

            // Translate point back
            return rotatedPoint + center;
        }

        private void AddXValue(double time, float value, float progressed, bool absolute = false)
        {

            // Ensure time is a multiple of deltaTime
            if (time % deltaIncrement != 0)
            {
                // Handle the case where time is not a multiple of deltaTime
                // Option 1: Adjust time to the nearest multiple of deltaTime
                time = Math.Ceiling(time / deltaIncrement) * deltaIncrement;

                // Option 2: Throw an exception
                // throw new ArgumentException("Time must be a multiple of deltaTime.");
            }

            if (positionX.ContainsKey(time))
            {

                if (absolute)
                    positionX[time] = value;
                else
                {
                    positionX[time] += progressed;
                }

            }
            else
            {

                float lastValue = getLastX(time);

                if (absolute)
                    positionX.Add(time, value);
                else
                    positionX.Add(time, lastValue + value);

            }
        }



        private void AddYValue(double time, float value, float progressed, bool absolute = false)
        {

            // Ensure time is a multiple of deltaTime
            if (time % deltaIncrement != 0)
            {
                // Handle the case where time is not a multiple of deltaTime
                // Option 1: Adjust time to the nearest multiple of deltaTime
                time = Math.Ceiling(time / deltaIncrement) * deltaIncrement;

                // Option 2: Throw an exception
                // throw new ArgumentException("Time must be a multiple of deltaTime.");
            }

            if (positionY.ContainsKey(time))
            {

                if (absolute)
                    positionY[time] = value;
                else
                {
                    positionY[time] += progressed;
                }

            }
            else
            {

                float lastValue = getLastY(time);

                if (absolute)
                    positionY.Add(time, value);
                else
                    positionY.Add(time, lastValue + value);

            }

        }

        private float getLastX(double currentTime)
        {
            if (positionX.Count == 0)
            {
                return 0; // Or your default value
            }

            var keys = positionX.Keys.ToList();
            int left = 0;
            int right = keys.Count - 1;
            double lastKey = -1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (keys[mid] < currentTime)
                {
                    lastKey = keys[mid];
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return lastKey != -1 ? positionX[lastKey] : 0;
        }


        private float getLastY(double currentTime)
        {
            if (positionY.Count == 0)
            {
                return 0; // Or your default value
            }

            var keys = positionY.Keys.ToList();
            int left = 0;
            int right = keys.Count - 1;
            double lastKey = -1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (keys[mid] < currentTime)
                {
                    lastKey = keys[mid];
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return lastKey != -1 ? positionY[lastKey] : 0;
        }


        private void easeProgressAbsolute(OsbEasing ease, double start, double end, Vector2 startPos, Vector2 endPos)
        {


            double duration = Math.Max(end - start, 0); // Ensure non-negative duration
            double deltaTime = 0;
            Vector2 lastPos = startPos; // Keep track of the last position to calculate the delta

            double progress = 0;
            do
            {
                deltaTime += deltaIncrement; // Increment time by deltaIncrement
                progress = deltaTime / duration; // Normalized time [0, 1]
                progress = Math.Min(progress, 1);       // Clamp progress to 1 to avoid overshooting

                float t = (float)ease.Ease(progress);   // Apply easing function

                Vector2 newPos = Vector2.Lerp(startPos, endPos, t); // Interpolated position
                Vector2 movement = newPos - lastPos;               // Delta movement

                // Apply the delta movement
                AddXValue(start + deltaTime, movement.X, newPos.X);
                AddYValue(start + deltaTime, movement.Y, newPos.Y);


                lastPos = newPos;   // Update lastPos for the next iteration
            } while (progress < 1);

        }

        private void easeProgressRelative(OsbEasing ease, double start, double end, Vector2 offset)
        {
            Vector2 startPos = new Vector2(0, 0); // Assuming starting at origin; replace with actual start if different
            Vector2 endPos = startPos + offset;   // The final desired position

            double duration = Math.Max(end - start, 0); // Ensure non-negative duration
            double deltaTime = 0;
            Vector2 lastPos = startPos; // Keep track of the last position to calculate the delta

            double progress = 0;
            do
            {
                deltaTime += deltaIncrement; // Increment time by deltaIncrement
                progress = deltaTime / duration; // Normalized time [0, 1]
                progress = Math.Min(progress, 1);       // Clamp progress to 1 to avoid overshooting

                float t = (float)ease.Ease(progress);   // Apply easing function

                Vector2 newPos = Vector2.Lerp(startPos, endPos, t); // Interpolated position
                Vector2 movement = newPos - lastPos;               // Delta movement

                // Apply the delta movement
                AddXValue(start + deltaTime, movement.X, newPos.X);
                AddYValue(start + deltaTime, movement.Y, newPos.Y);


                lastPos = newPos;   // Update lastPos for the next iteration
            } while (progress < 1);

        }


        public Vector2 PositionAt(double time)
        {
            return new Vector2(getLastX(time), getLastY(time));
        }

        public Vector2 ScaleAt(double currentTime)
        {
            return originSprite.ScaleAt(currentTime);
        }

        public float RotationAt(double currentTIme)
        {
            return originSprite.RotationAt(currentTIme);
        }
    }
}