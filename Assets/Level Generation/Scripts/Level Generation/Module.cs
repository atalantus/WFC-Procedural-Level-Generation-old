using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Scriptable Object asset for one specific module
    /// </summary>
    [CreateAssetMenu(fileName = "New Module", menuName = "WFC Level Generation/Module")]
    public class Module : ScriptableObject
    {
        /// <summary>
        /// The module`s game object
        /// </summary>
        public GameObject moduleGO;

        /// <summary>
        /// The module`s face connections starting with the face facing behind (forward, up, right, back, down, left)
        /// </summary>
        public int[] faceConnections = new int[6];

        /// <summary>
        /// Checks this module for a specific face filter
        /// </summary>
        /// <param name="filter">The filter</param>
        /// <returns>Does this model depend on the given face filter</returns>
        public bool CheckModule(FaceFilter filter)
        {
            // Get receiving face index of this module
            var face = ((int) filter.faceDirection + 3) % 6;

            // Check if module matches a given filter
            return faceConnections[face] == filter.filterId;
        }
    }

    /// <summary>
    /// Face filter
    /// </summary>
    public struct FaceFilter
    {
        /// <summary>
        /// The face direction
        /// </summary>
        public FaceDirections faceDirection;

        /// <summary>
        /// The face directions
        /// </summary>
        public enum FaceDirections
        {
            Forward = 0,
            Up = 1,
            Right = 2,
            Back = 3,
            Down = 4,
            Left = 5
        }

        /// <summary>
        /// The face type that gets filtered out
        /// </summary>
        public int filterId;

        public FaceFilter(FaceDirections faceDirection, int filterId)
        {
            this.faceDirection = faceDirection;
            this.filterId = filterId;
        }

        public override string ToString()
        {
            return $"({faceDirection.ToString()}, {filterId})";
        }
    }
}