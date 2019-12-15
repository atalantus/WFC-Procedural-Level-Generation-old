using System.Collections.Generic;
using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Scriptable Object asset for one specific module
    /// </summary>
    [CreateAssetMenu(fileName = "New Module", menuName = "Map Generation/Module")]
    public class Module : ScriptableObject
    {
        /// <summary>
        /// The module`s game object
        /// </summary>
        public GameObject moduleGO;
        
        /// <summary>
        /// The module`s face connections starting with the face facing behind (behind, over, right, front, under, left)
        /// </summary>
        public int[] faceConnections = new int[6];

        /// <summary>
        /// Checks this module for a specific face filter
        /// </summary>
        /// <param name="filter">The filter</param>
        /// <returns>Does this model depend on the given face filter</returns>
        public bool CheckModule(FaceFilter filter)
        {
            //Debug.Log($"Checking {moduleGO.name} for face filter {filter.FaceIndex}, {filter.FilterType}");
            
            // Get receiving face index of this module
            var face = (filter.FaceIndex + 3) % 6;

            // Check if module matches a given filter
            return faceConnections[face] == filter.FilterID;
        }
    }

    /// <summary>
    /// Face filter
    /// </summary>
    public struct FaceFilter
    {
        /// <summary>
        /// The face`s index (See <see cref="Module.faceConnections"/>)
        /// </summary>
        public int FaceIndex;
        
        /// <summary>
        /// The face type that gets filtered out
        /// </summary>
        public int FilterID;

        public FaceFilter(int faceIndex, int filterId)
        {
            FaceIndex = faceIndex;
            FilterID = filterId;
        }
    }
}