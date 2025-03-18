/* 
Copyright (c) 2025 RussDev7

This source is subject to the GNU General Public License v3.0 (GPLv3).
See https://www.gnu.org/licenses/gpl-3.0.html.

THIS PROGRAM IS FREE SOFTWARE: YOU CAN REDISTRIBUTE IT AND/OR MODIFY 
IT UNDER THE TERMS OF THE GNU GENERAL PUBLIC LICENSE AS PUBLISHED BY 
THE FREE SOFTWARE FOUNDATION, EITHER VERSION 3 OF THE LICENSE, OR 
(AT YOUR OPTION) ANY LATER VERSION.

THIS PROGRAM IS DISTRIBUTED IN THE HOPE THAT IT WILL BE USEFUL, 
BUT WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF 
MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE. SEE THE 
GNU GENERAL PUBLIC LICENSE FOR MORE DETAILS.
*/

using System.Security.Cryptography;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Numerics;
using System.Linq;
using System.Text;
using System.IO;
using System;

using static WorldEdit.WorldUtils;

using Vector3 = Microsoft.Xna.Framework.Vector3; // For testing purposes.

public class WorldEdit
{
    /// <summary>
    /// 
    /// Welcome to the World Edit C# Class!
    /// 
    /// This project is almost ready to go right out of the box!
    /// The following require your attention:
    /// 
    /// Class Definitions:
    /// 
    /// 'WorldHeights'
    /// 'ItemIDValues'
    /// 'AirID'
    /// 'LogID'
    /// 'LeavesID'
    /// 'WandItemID'
    /// 
    /// Networking Definitions:
    /// 
    /// 'IsNetworkSessionActive'
    ///
    /// World Utilities:
    /// 
    /// 'GetUsersCursorLocation'
    /// 'GetUsersLocation'
    /// 'GetUsersHeldItem'
    /// 'GetBlockFromLocation'
    /// 'PlaceBlock'
    /// 'TeleportUser'
    /// 
    /// </summary>

    // You need to implement 'WorldHeights', 'ItemIDValues', 'AirID', 'LogID', 'LeavesID', and 'WandItemID' support manually!
    #region Class Definitions

    /// <summary>
    /// 
    /// You need to implement these values for your project!
    /// 
    /// </summary>

    public static Tuple<int, int> WorldHeights = new Tuple<int, int>(64, -64); // Top -> Bottom.
    public static Tuple<int, int> ItemIDValues = new Tuple<int, int>(0, 93);   // Min -> Max.
    public static int AirID = 0;
    public static int LogID = 17;
    public static int LeavesID = 18;
    public static int WandItemID = 39;
    ///

    #region Definitions

    // Define the main hashsets and their stacks. // Hashsets increase speed and removes unnecessary duplicates.
    // The undo and redo stacks use a third integer that's used as a guid to prevent removing duplicate regions.
    public static Stack<HashSet<Tuple<Vector3, int, int>>> UndoStack = new Stack<HashSet<Tuple<Vector3, int, int>>>();
    public static Stack<HashSet<Tuple<Vector3, int, int>>> RedoStack = new Stack<HashSet<Tuple<Vector3, int, int>>>();
    public static HashSet<Tuple<Vector3, int>> copiedRegion = new HashSet<Tuple<Vector3, int>>();
    public static HashSet<Tuple<Vector3, int>> copiedStackRegion = new HashSet<Tuple<Vector3, int>>();

    public class Region
    {
        // Save vectors including absolute positions.
        public Vector3 AbsolutePosition1 { get; set; }
        public Vector3 AbsolutePosition2 { get; set; }
        public Vector3 Position1 { get; set; }
        public Vector3 Position2 { get; set; }

        // Property to get the center of the region.
        public Vector3 Center => (Position1 + Position2) / 2;

        // Constructor to initialize the region with two vectors.
        public Region(Vector3 corner1, Vector3 corner2)
        {
            // Set the absolute positions.
            AbsolutePosition1 = corner1;
            AbsolutePosition2 = corner2;

            // Set the values based on the min and max.
            Position1 = Vector3.Min(corner1, corner2);
            Position2 = Vector3.Max(corner1, corner2);
        }
    }
    #endregion

    #endregion

    // You need to implement 'IsNetworkSessionActive' support manually!
    #region Networking Definitions

    /// <summary>
    /// 
    /// You need to implement these values for your project!
    /// 
    /// </summary>

    // Implement a feature to check whether the current game session is active or not.
    public static bool IsNetworkSessionActive() => (DNA.CastleMinerZ.CastleMinerZGame.Instance.CurrentNetworkSession != null);
    ///

    #endregion

    // You need to implement 'GetUsersCursorLocation', 'GetUsersLocation', 'GetUsersHeldItem', 'GetBlockFromLocation', 'PlaceBlock', and 'TeleportUser' support manually!
    #region World Utilities

    public class WorldUtils
    {
        /// <summary>
        /// 
        /// You need to implement these functions for your project!
        /// 
        /// </summary>

        // Implement a feature to gather the games cursor location. This is the in reach block location the user can interact with.
        public static Vector3 GetUsersCursorLocation() => DNA.CastleMinerZ.UI.InGameHUD.Instance.ConstructionProbe._worldIndex;

        // Implement a feature to get the users location.
        public static Vector3 GetUsersLocation() => DNA.CastleMinerZ.CastleMinerZGame.Instance.LocalPlayer.LocalPosition;

        // Implement a feature to get the currently held item from the user.
        public static int GetUsersHeldItem() => (int)DNA.CastleMinerZ.CastleMinerZGame.Instance.LocalPlayer.PlayerInventory.ActiveInventoryItem.ItemClass.ID;

        // Implement a feature to gather a block ID from a specified vector3 (XYZ) location.
        public static int GetBlockFromLocation(Vector3 location) => (int)DNA.CastleMinerZ.UI.InGameHUD.GetBlock(new DNA.IntVector3((int)location.X, (int)location.Y, (int)location.Z));

        // Implement a feature for placing a block ID at a specified vector3 (XYZ) location.
        public static void PlaceBlock(Vector3 location, int block) => DNA.CastleMinerZ.Net.AlterBlockMessage.Send(
            (DNA.Net.GamerServices.LocalNetworkGamer)DNA.CastleMinerZ.CastleMinerZGame.Instance.LocalPlayer.Gamer,
            new DNA.IntVector3((int)location.X, (int)location.Y, (int)location.Z),
            (DNA.CastleMinerZ.Terrain.BlockTypeEnum)block
        );

        // Implement a feature for teleporting the user to a specified vector3 (XYZ) location.
        public static void TeleportUser(Vector3 location, bool spawnOnTop) => DNA.CastleMinerZ.CastleMinerZGame.Instance.GameScreen.TeleportToLocation(
            new DNA.IntVector3((int)location.X, (int)location.Y, (int)location.Z),
            spawnOnTop
        );
        ///

        #region Direction Helpers

        public enum Direction
        {
            posX,
            negX,
            posZ,
            negZ,
            Up,
            Down
        }

        public static Direction GetFacingDirection(Vector3 regionLocation, Vector3 cursorLocation)
        {
            // Calculate absolute differences for X, Y, and Z.
            float deltaX = Math.Abs(regionLocation.X - cursorLocation.X);
            float deltaY = Math.Abs(regionLocation.Y - cursorLocation.Y);
            float deltaZ = Math.Abs(regionLocation.Z - cursorLocation.Z);

            // Check if cursorLocation is directly above or below regionLocation.
            if (deltaX < float.Epsilon && deltaZ < float.Epsilon)
            {
                return cursorLocation.Y > regionLocation.Y ? Direction.Up : Direction.Down;
            }

            // Get the direction based on the largest absolute difference.
            if (deltaY > deltaX && deltaY > deltaZ)
            {
                return cursorLocation.Y > regionLocation.Y ? Direction.Up : Direction.Down;
            }
            else if (deltaX > deltaZ)
            {
                return regionLocation.X > cursorLocation.X ? Direction.negX : Direction.posX;
            }
            else
            {
                return regionLocation.Z > cursorLocation.Z ? Direction.negZ : Direction.posZ;
            }
        }

        #endregion

        #region Utility Helpers

        public static int GetRandomBlockFromPattern(string pattern)
        {
            if (!pattern.Contains(',')) { return int.Parse(pattern); }
            int[] numbers = Array.ConvertAll(pattern.Split(','), int.Parse);

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] randomNumber = new byte[4];
                rng.GetBytes(randomNumber);

                int result = BitConverter.ToInt32(randomNumber, 0);
                return numbers[Math.Abs(result % numbers.Length)];
            }
        }

        public static int GenerateRandomNumber(int min, int max)
        {
            // Ensure that the input values are valid.
            if (min > max)
                return min; // If min is greater than max, return min.
            if (min == max)
                return min; // If min is equal to max, return min.

            // Use RNGCryptoServiceProvider to generate random bytes.
            using (var rng = new RNGCryptoServiceProvider())
            {
                var data = new byte[8];
                rng.GetBytes(data);

                // Convert the generated bytes to an integer (use startIndex: 0 for the entire byte array).
                int generatedValue = Math.Abs(BitConverter.ToInt32(data, startIndex: 0));

                // Calculate the difference between max and min.
                int diff = max - min;

                // Use modulo to ensure the generated value is within the specified range.
                int mod = generatedValue % diff;

                // Shift the normalized value to be within the specified range.
                int normalizedNumber = min + mod;

                return normalizedNumber;
            }
        }

        public static HashSet<Vector3> ExtractVector3HashSet(HashSet<Tuple<Vector3, int>> tupleHashSet)
        {
            // Rebuild the hashset removing Item2 (block id) and only select Item1 (Vector3).
            // Avoid using LINQ's overhead to improve the speed.

            HashSet<Vector3> vector3HashSet = new HashSet<Vector3>();

            foreach (var tuple in tupleHashSet)
            {
                vector3HashSet.Add(tuple.Item1);
            }

            return vector3HashSet;
        }

        // Check if the rotation angle is valid.
        public static bool IsValidRotation(int rotation)
        {
            return rotation == 0 || rotation == -0 || rotation == 90 || rotation == -90 || rotation == 180 || rotation == -180 || rotation == 240 || rotation == -240 || rotation == 360 || rotation == -360;
        }

        // Check if the shape is valid.
        public static bool IsValidBrushShape(string shape)
        {
            return shape == "floor" || shape == "cube" || shape == "sphere" || shape == "ring" || shape == "pyramid" || shape == "cone" || shape == "cylinder" || shape == "diamond" || shape == "tree" || shape == "snow"|| shape == "schem" || shape == "floodfill";
        }
        #endregion

        #region Math Helpers

        // Method to check if a position is within the region defined by start and end points.
        public static bool IsWithinRegion(Vector3 position, Vector3 start, Vector3 end)
        {
            return position.X >= Math.Min(start.X, end.X) && position.X <= Math.Max(start.X, end.X) &&
                   position.Y >= Math.Min(start.Y, end.Y) && position.Y <= Math.Max(start.Y, end.Y) &&
                   position.Z >= Math.Min(start.Z, end.Z) && position.Z <= Math.Max(start.Z, end.Z);
        }

        public static bool IsWithinWorldBounds(Vector3 pos, int additionalHeight = 0, int additionalDepth = 0)
        {
            return pos.Y <= (WorldHeights.Item1 + additionalHeight) && pos.Y >= (WorldHeights.Item2 - additionalDepth);
        }

        public static bool IsWithinWorldBounds(int yPos, int additionalHeight = 0, int additionalDepth = 0)
        {
            return yPos <= (WorldHeights.Item1 + additionalHeight) && yPos >= (WorldHeights.Item2 - additionalDepth);
        }

        public static Vector3 GetRegionCenter()
        {
            float xSum = 0, ySum = 0, zSum = 0;
            int count = copiedRegion.Count;

            foreach (var item in copiedRegion)
            {
                xSum += item.Item1.X;
                ySum += item.Item1.Y;
                zSum += item.Item1.Z;
            }

            // Calculate the average of all vectors to find the center.
            return new Vector3(xSum / count, ySum / count, zSum / count);
        }

        public static int CalculateBlockCount(Vector3 pos1, Vector3 pos2)
        {
            int xCount = Math.Abs((int)pos2.X - (int)pos1.X) + 1;
            int yCount = Math.Abs((int)pos2.Y - (int)pos1.Y) + 1;
            int zCount = Math.Abs((int)pos2.Z - (int)pos1.Z) + 1;
        
            return xCount * yCount * zCount;
        }

        public bool AreInSameQuadrant(Vector3 v1, Vector3 v2)
        {
            return (Math.Sign(v1.X) == Math.Sign(v2.X)) &&
                   (Math.Sign(v1.Y) == Math.Sign(v2.Y)) &&
                   (Math.Sign(v1.Z) == Math.Sign(v2.Z));
        }

        public static double LengthSq(double x, double z)
        {
            return x * x + z * z;
        }

        public static double LengthSq(double x, double y, double z)
        {
            return x * x + y * y + z * z;
        }

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
        #endregion

        #region Enum Helpers

        public static string GetClosestEnumValues<T>(string input) where T : struct, Enum
        {
            var enumNames = Enum.GetNames(typeof(T));
            var enumValues = Enum.GetValues(typeof(T)).Cast<int>().ToArray();

            return string.Join(",", input.Split(',')
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => ProcessEnumInput<T>(name, enumNames, enumValues))
                .Select(value => value.ToString()));
        }

        private static int ProcessEnumInput<T>(string input, string[] enumNames, int[] enumValues) where T : struct, Enum
        {
            if (int.TryParse(input, out int intValue) && enumValues.Contains(intValue))
            {
                return intValue; // If input is a valid enum integer, return it directly.
            }

            // If the input is a string, find the closest match.
            return FindClosestEnumValue<T>(input, enumNames);
        }

        private static int FindClosestEnumValue<T>(string input, string[] enumNames) where T : struct, Enum
        {
            var closestMatch = enumNames.OrderBy(name => GetLevenshteinDistance(name, input)).First();

            if (Enum.TryParse(closestMatch, true, out T result))
            {
                return Convert.ToInt32(result);
            }

            return ItemIDValues.Item1; // Return the minimum ID (or invalid value) if not found.
        }

        // Levenshtein Distance Algorithm.
        private static int GetLevenshteinDistance(string s1, string s2)
        {
            int len1 = s1.Length;
            int len2 = s2.Length;
            var dp = new int[len1 + 1, len2 + 1];

            for (int i = 0; i <= len1; i++)
                for (int j = 0; j <= len2; j++)
                    if (i == 0) dp[i, j] = j;
                    else if (j == 0) dp[i, j] = i;
                    else dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + (s1[i - 1] == s2[j - 1] ? 0 : 1));

            return dp[len1, len2];
        }
        #endregion

        #region XnaPerlinNoise Helper

        public class XnaPerlinNoise
        {
            public float ComputeNoise(Vector3 position)
            {
                // Extract the individual components of the position vector.
                float posX = position.X;
                float posY = position.Y;
                float posZ = position.Z;

                // Calculate integer floor values for each coordinate.
                int cellX = (posX > 0f) ? (int)posX : (int)posX - 1;
                int cellY = (posY > 0f) ? (int)posY : (int)posY - 1;
                int cellZ = (posZ > 0f) ? (int)posZ : (int)posZ - 1;

                // Wrap the cell coordinates within 255 (used for permutation table lookup).
                int permX = cellX & 255;
                int permY = cellY & 255;
                int permZ = cellZ & 255;

                // Compute local positions within the grid cell.
                float localX = posX - (float)cellX;
                float localY = posY - (float)cellY;
                float localZ = posZ - (float)cellZ;

                // Compute fade curves for smooth transitions.
                float fadeX = localX * localX * localX * (localX * (localX * 6f - 15f) + 10f);
                float fadeY = localY * localY * localY * (localY * (localY * 6f - 15f) + 10f);
                float fadeZ = localZ * localZ * localZ * (localZ * (localZ * 6f - 15f) + 10f);

                // Hash coordinates to get permutation indices.
                int hashXY = _permute[permX] + permY;
                int hashXYZ = _permute[hashXY] + permZ;
                int hashXY1Z = _permute[hashXY + 1] + permZ;
                int hashX1Y = _permute[permX + 1] + permY;
                int hashX1YZ = _permute[hashX1Y] + permZ;
                int hashX1Y1Z = _permute[hashX1Y + 1] + permZ;

                // Calculate gradient dot products for each corner of the cell.
                float gradX1 = localX - 1f;
                float gradY1 = localY - 1f;
                float gradZ1 = localZ - 1f;

                int[] gradientVector = s_gradientVectors[_permute[hashXYZ] & 15];
                float dot000 = localX * gradientVector[0] + localY * gradientVector[1] + localZ * gradientVector[2];

                gradientVector = s_gradientVectors[_permute[hashX1YZ] & 15];
                float dot100 = gradX1 * gradientVector[0] + localY * gradientVector[1] + localZ * gradientVector[2];

                gradientVector = s_gradientVectors[_permute[hashXY1Z] & 15];
                float dot010 = localX * gradientVector[0] + gradY1 * gradientVector[1] + localZ * gradientVector[2];

                gradientVector = s_gradientVectors[_permute[hashX1Y1Z] & 15];
                float dot110 = gradX1 * gradientVector[0] + gradY1 * gradientVector[1] + localZ * gradientVector[2];

                gradientVector = s_gradientVectors[_permute[hashXYZ + 1] & 15];
                float dot001 = localX * gradientVector[0] + localY * gradientVector[1] + gradZ1 * gradientVector[2];

                gradientVector = s_gradientVectors[_permute[hashX1YZ + 1] & 15];
                float dot101 = gradX1 * gradientVector[0] + localY * gradientVector[1] + gradZ1 * gradientVector[2];

                gradientVector = s_gradientVectors[_permute[hashXY1Z + 1] & 15];
                float dot011 = localX * gradientVector[0] + gradY1 * gradientVector[1] + gradZ1 * gradientVector[2];

                gradientVector = s_gradientVectors[_permute[hashX1Y1Z + 1] & 15];
                float dot111 = gradX1 * gradientVector[0] + gradY1 * gradientVector[1] + gradZ1 * gradientVector[2];

                // Interpolate between gradient values.
                float interpX0 = dot000 + fadeX * (dot100 - dot000);
                float interpX1 = dot010 + fadeX * (dot110 - dot010);
                float interpX2 = dot001 + fadeX * (dot101 - dot001);
                float interpX3 = dot011 + fadeX * (dot111 - dot011);

                // Interpolate the corner values.
                float interpY0 = interpX0 + fadeY * (interpX1 - interpX0);
                float interpY1 = interpX2 + fadeY * (interpX3 - interpX2);

                // Final interpolation.
                return interpY0 + fadeZ * (interpY1 - interpY0);
            }

            // Initialize permutation table.
            private void Initalize(Random r)
            {
                for (int i = 0; i < 256; i++)
                {
                    _permute[256 + i] = (_permute[i] = r.Next(256));
                }
                for (int j = 0; j < 512; j++)
                {
                    _permute[512 + j] = _permute[j];
                }
            }

            // Constructors.
            public XnaPerlinNoise()
            {
                this.Initalize(new Random());
            }

            public XnaPerlinNoise(Random r)
            {
                this.Initalize(r);
            }

            // Gradient vectors for the noise function.
            private static readonly int[][] s_gradientVectors = new int[][]
            {
            new int[] { 0, -1, -1, -1 },
            new int[] { -1, 0, -1, -1 },
            new int[] { 1, 0, -1, -1 },
            new int[] { 0, 1, -1, -1 },
            new int[] { -1, -1, 0, -1 },
            new int[] { 1, -1, 0, -1 },
            new int[] { -1, 1, 0, -1 },
            new int[] { 1, 1, 0, -1 },
            new int[] { 0, -1, 1, -1 },
            new int[] { -1, 0, 1, -1 },
            new int[] { 1, 0, 1, -1 },
            new int[] { 0, 1, 1, -1 },
            new int[] { 1, 1, 0, 1 },
            new int[] { -1, 1, 0, 1 },
            new int[] { 0, -1, -1, 1 },
            new int[] { 0, -1, 1, 1 },
            new int[] { -1, -1, -1, 0 },
            new int[] { 1, -1, -1, 0 },
            new int[] { -1, 1, -1, 0 },
            new int[] { 1, 1, -1, 0 },
            new int[] { -1, -1, 1, 0 },
            new int[] { 1, -1, 1, 0 },
            new int[] { -1, 1, 1, 0 },
            new int[] { 1, 1, 1, 0 },
            new int[] { -1, 0, -1, 1 },
            new int[] { 1, 0, -1, 1 },
            new int[] { 0, 1, -1, 1 },
            new int[] { -1, -1, 0, 1 },
            new int[] { 1, -1, 0, 1 },
            new int[] { -1, 0, 1, 1 },
            new int[] { 1, 0, 1, 1 },
            new int[] { 0, 1, 1, 1 }
            };

            // Permutation table for the noise function.
            private static readonly int[] _permute = new int[1024];
        }
        #endregion
    }
    #endregion

    /// <summary>
    /// 
    /// 'Undo'
    /// 'Redo'
    /// 'Clear'
    /// 
    /// </summary>
    #region General Methods

    #region Undo

    // Push nothng to the undo stack to "skip" this data.
    public static void SaveUndo()
    {
        HashSet<Tuple<Vector3, int, int>> actionsBuilder = new HashSet<Tuple<Vector3, int, int>>();

        // Add builder to new redo.
        UndoStack.Push(actionsBuilder);
    }

    // Get the existing block data and push it to the undo stack.
    public static void SaveUndo(HashSet<Vector3> region, int saveBlock = -1, int ignoreBlock = -1)
    {
        HashSet<Tuple<Vector3, int, int>> actionsBuilder = new HashSet<Tuple<Vector3, int, int>>();
        bool wasGUIDCreated = false; // Prevents making a new guid on non-duplicate hashsets.
        bool useForAll = false;      // Skip further stack checking to increase performance.
        int randomGUID = 0;          // Define a starting integer.

        // Iterate through all vectors within the region.
        foreach (Vector3 i in region)
        {
            // Get the block ID from regions location within the world.
            int worldBlock = GetBlockFromLocation(i);

            // If a save block was specified, only save the locations of that block id.
            if (saveBlock != -1 && worldBlock != saveBlock) continue;

            // If ignore block was specified, skip locations matching the block id.
            if (ignoreBlock != -1 && worldBlock == ignoreBlock) continue;

            // Define new data.
            var vectorData = new Tuple<Vector3, int, int>(i, worldBlock, 0);

            // Check if the hashset already contains this data.
            if (useForAll || (UndoStack.Count > 0 && UndoStack.Peek().Contains(vectorData)))
            {
                // Generate a new guid only once.
                if (!wasGUIDCreated)
                {
                    wasGUIDCreated = true; // Prevents making a new guid on non-duplicate hashsets.
                    randomGUID = Guid.NewGuid().GetHashCode();
                }

                useForAll = true;          // Skip further stack checking to increase performance.
                vectorData = new Tuple<Vector3, int, int>(i, worldBlock, randomGUID);
            }

            actionsBuilder.Add(vectorData);
        }

        // Add builder to new redo.
        UndoStack.Push(actionsBuilder);
    }

    // Push the new block data to the undo stack.
    public static void SaveUndo(HashSet<Tuple<Vector3, int>> currentActions)
    {
        HashSet<Tuple<Vector3, int, int>> actionsBuilder = new HashSet<Tuple<Vector3, int, int>>();
        bool wasGUIDCreated = false; // Prevents making a new guid on non-duplicate hashsets.
        bool useForAll = false;      // Skip further stack checking to increase performance.
        int randomGUID = 0;          // Define a starting integer.

        // Iterate through all vectors within the region.
        foreach (var action in currentActions)
        {
            Vector3 position = action.Item1;
            int blockID = action.Item2;

            // Define new data.
            var vectorData = new Tuple<Vector3, int, int>(position, blockID, 0);

            // Check if the undo stack already contains this data.
            if (useForAll || (UndoStack.Count > 0 && UndoStack.Peek().Contains(vectorData)))
            {
                // Generate a new guid only once.
                if (!wasGUIDCreated)
                {
                    wasGUIDCreated = true; // Prevents making a new guid on non-duplicate hashsets.
                    randomGUID = Guid.NewGuid().GetHashCode();
                }

                useForAll = true;          // Skip further stack checking to increase performance.
                vectorData = new Tuple<Vector3, int, int>(position, blockID, randomGUID);
            }

            actionsBuilder.Add(vectorData);
        }

        // Add builder to new redo.
        UndoStack.Push(actionsBuilder);
    }
    #endregion

    #region Redo

    public static HashSet<Tuple<Vector3, int>> LoadUndo()
    {
        // Ensure undo exists.
        if (UndoStack.Count == 0)
        {
            throw new InvalidOperationException("No undo actions available.");
        }

        // Pop and push the current actions to the redo stack for each undo action.
        var actions = UndoStack.Pop();
        RedoStack.Push(actions);

        // Pop the last action set from undo stack and reverse it.
        actions = UndoStack.Pop();

        // Push the actions onto the redo stack to allow redoing.
        RedoStack.Push(actions);

        // Convert from Tuple<Vector3, int, int> to Tuple<Vector3, int> (ignoring the third value).
        HashSet<Tuple<Vector3, int>> result = new HashSet<Tuple<Vector3, int>>();
        foreach (var action in actions)
            result.Add(new Tuple<Vector3, int>(action.Item1, action.Item2));

        // Send back the undo data.
        return result;
    }

    public static HashSet<Tuple<Vector3, int>> LoadRedo()
    {
        // Ensure undo exists.
        if (RedoStack.Count == 0)
        {
            throw new InvalidOperationException("No redo actions available.");
        }

        // Pop and push the current actions to the undo stack for each redo action.
        var actions = RedoStack.Pop();
        UndoStack.Push(actions);

        // Pop the last action set from undo stack and reverse it.
        actions = RedoStack.Pop();

        // Push the actions onto the redo stack to allow redoing.
        UndoStack.Push(actions);

        // Convert from Tuple<Vector3, int, int> to Tuple<Vector3, int> (ignoring the third value).
        HashSet<Tuple<Vector3, int>> result = new HashSet<Tuple<Vector3, int>>();
        foreach (var action in actions)
            result.Add(new Tuple<Vector3, int>(action.Item1, action.Item2));
        
        // Send back the undo data.
        return result;
    }
    #endregion

    #region Clear

    public static void ClearRedo()
    {
        RedoStack.Clear();
    }
    public static void ClearUndo()
    {
        UndoStack.Clear();
    }
    #endregion

    #endregion

    /// <summary>
    /// 
    /// 'Ascend'
    /// 'Descend'
    /// 'Ceil'
    /// 'Thru'
    /// 
    /// </summary>
    #region Navigation Methods

    #region Ascend

    public static Vector3 GetAscendingVector(Vector3 pos)
    {
        for (int y = (int)pos.Y; y < WorldHeights.Item1; y++)                         // Start at current Y, go up.
        {
            if (GetBlockFromLocation(new Vector3(pos.X, y, pos.Z)) != AirID)          // Stop at non air block.
            {
                for (int y2 = y + 1; y2 < WorldHeights.Item1; y2++)                   // Continue up to find a valid block to stand on.
                {
                    if (GetBlockFromLocation(new Vector3(pos.X, y2, pos.Z)) == AirID) // Stop at air.
                    {
                        return new Vector3(pos.X, y2, pos.Z);                         // Return open space block.
                    }
                }
            }
        }
        return pos;                                                                   // If no valid position found, return original position.
    }
    #endregion

    #region Descend

    public static Vector3 GetDescendingVector(Vector3 pos)
    {
        for (int y = (int)pos.Y; y > WorldHeights.Item2; y--)                                 // Start at current Y, go down.
        {
            if (GetBlockFromLocation(new Vector3(pos.X, y, pos.Z)) != AirID)                  // Stop at none air block.
            {
                for (int y2 = y - 1; y2 > WorldHeights.Item2; y2--)                           // Continue down to find air.
                {
                    if (GetBlockFromLocation(new Vector3(pos.X, y2, pos.Z)) == AirID)         // Stop at air.
                    {
                        for (int y3 = y2 - 1; y3 > WorldHeights.Item2; y3--)                  // Continue down to find a valid block to stand on.
                        {
                            if (GetBlockFromLocation(new Vector3(pos.X, y3, pos.Z)) != AirID) // Stop at non air block.
                            {
                                return new Vector3(pos.X, y3 + 1, pos.Z);                     // Return open space above block.
                            }
                        }
                    }
                }
            }
        }
        return pos;                                                                           // If no valid position found, return original position.
    }
    #endregion

    #region Ceil

    public static Vector3 GetCeilingVector(Vector3 pos)
    {
        for (int y = WorldHeights.Item1; y > WorldHeights.Item2 - 1; y--)    // Start at max Y, go down.
        {
            if (GetBlockFromLocation(new Vector3(pos.X, y, pos.Z)) != AirID) // Stop at non air block.
            {
                return new Vector3(pos.X, y + 1f, pos.Z);                    // Return open space above block.
            }
        }
        return pos;                                                          // If no valid position found, return original position.
    }
    #endregion

    #region Thru

    public static Vector3 GetThruVector(Vector3 pos, Direction facingDirection)
    {
        // Step vector to move in the correct direction.
        Vector3 step = Vector3.Zero;

        if (facingDirection == Direction.Up) step = new Vector3(0, 1, 0);
        else if (facingDirection == Direction.Down) step = new Vector3(0, -1, 0);
        else if (facingDirection == Direction.posX) step = new Vector3(1, 0, 0);
        else if (facingDirection == Direction.negX) step = new Vector3(-1, 0, 0);
        else if (facingDirection == Direction.posZ) step = new Vector3(0, 0, 1);
        else if (facingDirection == Direction.negZ) step = new Vector3(0, 0, -1);

        // Start at the current position and move in the step direction.
        Vector3 currentPos = pos;
        while (IsWithinWorldBounds(currentPos))                    // Ensure we stay within world bounds.
        {
            currentPos += step;                                    // Move in the facing direction.

            if (GetBlockFromLocation(currentPos) != AirID)         // Continue moving when we find a none air block.
            {                                                      // This would be the wall to pass through.

                while (IsWithinWorldBounds(currentPos))            // Ensure we stay within world bounds.
                {
                    currentPos += step;                            // Move in the facing direction.

                    if (GetBlockFromLocation(currentPos) == AirID) // Air found. This is the other side of the wall.
                    {
                        currentPos += step;                        // Move once more in the facing direction.
                        return currentPos;                         // Return the first open space.
                    }
                }
            }
        }

        return pos;                                                // If no valid position found, return original position.
    }
    #endregion

    #endregion

    /// <summary>
    /// 
    /// 'Fill Region'
    /// 'Line'
    /// 'Walls'
    /// 'Smooth'
    /// 'Stack'
    /// 'Spell Words'
    /// 'Hollow'
    /// 'Fill'
    /// 'FloodFill'
    /// 'Wrap'
    /// 'Matrix'
    /// 'Snow'
    /// 'Forest'
    /// 'Tree'
    /// 
    /// </summary>
    #region Region Methods

    #region Fill Region

    public static HashSet<Vector3> FillRegion(Region region, bool hollow, int ignoreBlock = -1)
    {
        HashSet<Vector3> regionBlocks = new HashSet<Vector3>();

        for (int y = (int)region.Position1.Y; y <= (int)region.Position2.Y; ++y)
        {
            // Ensure Y is within the world's height constraints.
            if (y > WorldHeights.Item1 || y < WorldHeights.Item2)
                continue;

            for (int x = (int)region.Position1.X; x <= (int)region.Position2.X; ++x)
            {
                for (int z = (int)region.Position1.Z; z <= (int)region.Position2.Z; ++z)
                {
                    // If not hollow, add all blocks.
                    if (!hollow)
                    {
                        // If ignore block was specified, skip locations matching the block id.
                        Vector3 newPos = new Vector3(x, y, z);
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            regionBlocks.Add(newPos);
                    }
                    else
                    {
                        // Add only the boundary blocks.
                        bool isBoundary =
                            x == (int)region.Position1.X || x == (int)region.Position2.X || // X boundaries.
                            y == (int)region.Position1.Y || y == (int)region.Position2.Y || // Y boundaries.
                            z == (int)region.Position1.Z || z == (int)region.Position2.Z;   // Z boundaries.

                        if (isBoundary)
                        {
                            // If ignore block was specified, skip locations matching the block id.
                            Vector3 newPos = new Vector3(x, y, z);
                            if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                                regionBlocks.Add(newPos);
                        }
                    }
                }
            }
        }

        return regionBlocks;
    }
    #endregion

    #region Line

    public static HashSet<Vector3> MakeLine(Region region, int thickness)
    {
        // Important: Use absolute positions.
        Vector3 start = region.AbsolutePosition1;
        Vector3 end = region.AbsolutePosition2;

        HashSet<Vector3> lineBlocks = new HashSet<Vector3>();

        // Get direction vector.
        Vector3 direction = end - start;
        float maxComponent = Math.Max(Math.Abs(direction.X), Math.Max(Math.Abs(direction.Y), Math.Abs(direction.Z)));
        direction /= maxComponent; // Normalize while keeping integer precision.

        // Iterate through points along the line.
        for (int i = 0; i <= maxComponent; i++)
        {
            Vector3 point = start + direction * i;
            Vector3 roundedPoint = new Vector3((int)point.X, (int)point.Y, (int)point.Z);

            // Ensure Y is within the world's height constraints.
            if (roundedPoint.Y > WorldHeights.Item1 || roundedPoint.Y < WorldHeights.Item2)
                continue;

            lineBlocks.Add(new Vector3(roundedPoint.X, roundedPoint.Y, roundedPoint.Z));
        }

        // Account for the thickness by expanding the line into a 'ballooned' shape.
        if (thickness > 0)
            lineBlocks = GetBallooned(lineBlocks, thickness);

        return lineBlocks;
    }

    #region Balloon Math Helper

    private static HashSet<Vector3> GetBallooned(HashSet<Vector3> lineBlocks, int thickness)
    {
        HashSet<Vector3> expandedBlocks = new HashSet<Vector3>();

        foreach (var block in lineBlocks)
        {
            // For each block, add surrounding blocks to create the thickness effect
            for (int xOffset = -thickness; xOffset <= thickness; xOffset++)
            {
                for (int yOffset = -thickness; yOffset <= thickness; yOffset++)
                {
                    // Ensure Y is within the world's height constraints.
                    int worldY = (int)block.Y + yOffset; // Calculate actual world Y position.
                    if (worldY > WorldHeights.Item1 || worldY < WorldHeights.Item2)
                        continue;

                    for (int zOffset = -thickness; zOffset <= thickness; zOffset++)
                    {
                        // Only add blocks that are within the radius of the original block
                        if (Math.Sqrt(xOffset * xOffset + yOffset * yOffset + zOffset * zOffset) <= thickness)
                        {
                            expandedBlocks.Add(block + new Vector3(xOffset, yOffset, zOffset));
                        }
                    }
                }
            }
        }

        return expandedBlocks;
    }
    #endregion

    #endregion

    #region Walls

    public static HashSet<Vector3> MakeWalls(Region region)
    {
        HashSet<Vector3> wallBlocks = new HashSet<Vector3>();

        // Get min/max values to form a bounding box.
        int minX = (int)Math.Min(region.Position1.X, region.Position2.X);
        int maxX = (int)Math.Max(region.Position1.X, region.Position2.X);
        int minY = (int)Math.Min(region.Position1.Y, region.Position2.Y);
        int maxY = (int)Math.Max(region.Position1.Y, region.Position2.Y);
        int minZ = (int)Math.Min(region.Position1.Z, region.Position2.Z);
        int maxZ = (int)Math.Max(region.Position1.Z, region.Position2.Z);

        // Iterate over the bounding box.
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Ensure Y is within the world's height constraints.
                if (y > WorldHeights.Item1 || y < WorldHeights.Item2)
                    continue;

                for (int z = minZ; z <= maxZ; z++)
                {
                    // Exclude full top and bottom layers but keep their edges.
                    bool isSideWall = (x == minX || x == maxX || z == minZ || z == maxZ);
                    bool isTopBottomEdge = (y == minY || y == maxY) && isSideWall;

                    if (isSideWall || isTopBottomEdge)
                    {
                        wallBlocks.Add(new Vector3(x, y, z));
                    }
                }
            }
        }

        return wallBlocks;
    }
    #endregion

    #region Smooth

    public static HashSet<Tuple<Vector3, int>> SmoothTerrain(Region region, int iterations)
    {
        // Build a dictionary mapping (x, z) -> (surfaceY, blockID).
        // We assume a nonzero blockID is part of the terrain.
        var topBlocks = new Dictionary<(int, int), Tuple<float, int>>();

        for (int x = (int)region.Position1.X; x <= (int)region.Position2.X; x++)
        {
            for (int z = (int)region.Position1.Z; z <= (int)region.Position2.Z; z++)
            {
                float maxY = float.MinValue;
                int blockID = 0;
                // Iterate over the y values in the region.
                for (int y = (int)region.Position1.Y; y <= (int)region.Position2.Y; y++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    int currentID = GetBlockFromLocation(pos);
                    // Here, we treat block id 0 as "air".
                    if (currentID != 0 && y > maxY)
                    {
                        maxY = y;
                        blockID = currentID;
                    }
                }
                if (maxY != float.MinValue)
                {
                    topBlocks[(x, z)] = Tuple.Create(maxY, blockID);
                }
            }
        }

        // Smooth the height map over the specified number of iterations.
        for (int iter = 0; iter < iterations; iter++)
        {
            var newTopBlocks = new Dictionary<(int, int), Tuple<float, int>>();
            foreach (var kvp in topBlocks)
            {
                var (x, z) = kvp.Key;
                float currentY = kvp.Value.Item1;
                int currentBlockID = kvp.Value.Item2;

                // Look at the four neighbors in 2D: north, south, east, and west.
                List<float> neighborYs = new List<float>();
                foreach (var offset in new List<(int, int)> { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var neighborKey = (x + offset.Item1, z + offset.Item2);
                    if (topBlocks.ContainsKey(neighborKey))
                    {
                        neighborYs.Add(topBlocks[neighborKey].Item1);
                    }
                }

                // Average the current Y with any neighbor Ys.
                float newY = currentY;
                if (neighborYs.Count > 0)
                {
                    newY = (currentY + neighborYs.Sum()) / (neighborYs.Count + 1);
                }

                newTopBlocks[(x, z)] = Tuple.Create(newY, currentBlockID);
            }
            topBlocks = newTopBlocks;
        }

        // Build the output HashSet.
        // We round the new Y to the nearest integer since block positions are discrete.
        var smoothedRegion = new HashSet<Tuple<Vector3, int>>();
        foreach (var kvp in topBlocks)
        {
            int x = kvp.Key.Item1;
            int z = kvp.Key.Item2;
            int smoothedY = (int)Math.Round(kvp.Value.Item1);
            int blockID = kvp.Value.Item2;
            smoothedRegion.Add(new Tuple<Vector3, int>(new Vector3(x, smoothedY, z), blockID));
        }
        return smoothedRegion;
    }
    #endregion

    #region Stack

    public static HashSet<Tuple<Vector3, int>> StackRegion(Region region, Direction facingDirection, int stackCount)
    {
        HashSet<Tuple<Vector3, int>> stackBlocks = new HashSet<Tuple<Vector3, int>>();

        // Offset for each stack (adjust as needed).
        Vector3 stackOffset = new Vector3(1, 1, 1);

        // Loop for each stack.
        for (int i = 1; i <= stackCount; i++)
        {
            // Get each XYZ.
            for (int y = (int)region.Position1.Y; y <= (int)region.Position2.Y; ++y)
            {
                for (int x = (int)region.Position1.X; x <= (int)region.Position2.X; ++x)
                {
                    for (int z = (int)region.Position1.Z; z <= (int)region.Position2.Z; ++z)
                    {
                        // Calculate the offset per direction and update original position with calculated position.
                        Vector3 regionOffset = new Vector3(x, y, z) + GetStackedRegionOffset(region, facingDirection, stackOffset, i);

                        // Ensure Y is within the world's height constraints.
                        if (regionOffset.Y > WorldHeights.Item1 || regionOffset.Y < WorldHeights.Item2)
                            continue;

                        // Save new location to stack region hashset.
                        stackBlocks.Add(new Tuple<Vector3, int>(regionOffset, GetBlockFromLocation(new Vector3(x, y, z))));

                        // Console.WriteLine("Stacked Region " + i + " - Original: " + new Vector3(x, y, z) + ", Offset: " + regionOffset);
                    }
                }
            }
        }

        return stackBlocks;
    }

    static Vector3 GetStackedRegionOffset(Region region, Direction facingDirection, Vector3 stackOffset, int stackIndex)
    {
        // Calculate the size of the original region.
        Vector3 regionSize = region.Position2 - region.Position1;

        // Initialize the region offset.
        Vector3 regionOffset = Vector3.Zero;

        // Determine the offset based on the stacking direction.
        if (facingDirection == Direction.Up)
        {
            regionOffset = new Vector3(0, (regionSize.Y + stackOffset.Y) * stackIndex, 0);
        }
        else if (facingDirection == Direction.Down)
        {
            regionOffset = new Vector3(0, -(regionSize.Y + stackOffset.Y) * stackIndex, 0);
        }
        else if (facingDirection == Direction.posX)
        {
            regionOffset = new Vector3((regionSize.X + stackOffset.X) * stackIndex, 0, 0);
        }
        else if (facingDirection == Direction.negX)
        {
            regionOffset = new Vector3(-(regionSize.X + stackOffset.X) * stackIndex, 0, 0);
        }
        else if (facingDirection == Direction.posZ)
        {
            regionOffset = new Vector3(0, 0, (regionSize.Z + stackOffset.Z) * stackIndex);
        }
        else if (facingDirection == Direction.negZ)
        {
            regionOffset = new Vector3(0, 0, -(regionSize.Z + stackOffset.Z) * stackIndex);
        }

        // Return the calculated region offset.
        return regionOffset;
    }
    #endregion

    #region Spell Words

    public static HashSet<Vector3> MakeWords(Vector3 pos, string wordString, bool flipAxes = false, bool rotate90 = false)
    {
        HashSet<Vector3> wordBlocks = new HashSet<Vector3>();

        // Check if the input string is "/paste".
        if (wordString == "/paste")
            wordString = Clipboard.GetText();

        #region Individual Characters

        // Letter patterns using '#' as block positions.
        Dictionary<char, string[]> letterPatterns = new Dictionary<char, string[]>
        {
            #region Letters
            { 'A', new string[]
            {
                "  #  ",
                " # # ",
                "#   #",
                "#####",
                "#   #"
            }},
            { 'B', new string[]
            {
                "#### ",
                "#   #",
                "#### ",
                "#   #",
                "#### "
            }},
            { 'C', new string[]
            {
                " ####",
                "#    ",
                "#    ",
                "#    ",
                " ####"
            }},
            { 'D', new string[]
            {
                "###  ",
                "#  # ",
                "#   #",
                "#  # ",
                "###  "
            }},
            { 'E', new string[]
            {
                "#####",
                "#    ",
                "#####",
                "#    ",
                "#####"
            }},
            { 'F', new string[]
            {
                "#####",
                "#    ",
                "#####",
                "#    ",
                "#    "
            }},
            { 'G', new string[]
            {
                " ####",
                "#    ",
                "#  ##",
                "#   #",
                " ####"
            }},
            { 'H', new string[]
            {
                "#   #",
                "#   #",
                "#####",
                "#   #",
                "#   #"
            }},
            { 'I', new string[]
            {
                " ### ",
                "  #  ",
                "  #  ",
                "  #  ",
                " ### "
            }},
            { 'J', new string[]
            {
                "  ###",
                "   # ",
                "   # ",
                "#  # ",
                " ##  "
            }},
            { 'K', new string[]
            {
                "#   #",
                "#  # ",
                "###  ",
                "#  # ",
                "#   #"
            }},
            { 'L', new string[]
            {
                "#    ",
                "#    ",
                "#    ",
                "#    ",
                "#####"
            }},
            { 'M', new string[]
            {
                "#   #",
                "## ##",
                "# # #",
                "#   #",
                "#   #"
            }},
            { 'N', new string[]
            {
                "#   #",
                "##  #",
                "# # #",
                "#  ##",
                "#   #"
            }},
            { 'O', new string[]
            {
                " ### ",
                "#   #",
                "#   #",
                "#   #",
                " ### "
            }},
            { 'P', new string[]
            {
                "#### ",
                "#   #",
                "#### ",
                "#    ",
                "#    "
            }},
            { 'Q', new string[]
            {
                " ### ",
                "#   #",
                "#   #",
                "#  ##",
                " ####"
            }},
            { 'R', new string[]
            {
                "#### ",
                "#   #",
                "#### ",
                "#  # ",
                "#   #"
            }},
            { 'S', new string[]
            {
                " ####",
                "#    ",
                " ### ",
                "    #",
                "#### "
            }},
            { 'T', new string[]
            {
                "#####",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  "
            }},
            { 'U', new string[]
            {
                "#   #",
                "#   #",
                "#   #",
                "#   #",
                " ### "
            }},
            { 'V', new string[]
            {
                "#   #",
                "#   #",
                "#   #",
                " # # ",
                "  #  "
            }},
            { 'W', new string[]
            {
                "#   #",
                "#   #",
                "# # #",
                "# # #",
                " # # "
            }},
            { 'X', new string[]
            {
                "#   #",
                " # # ",
                "  #  ",
                " # # ",
                "#   #"
            }},
            { 'Y', new string[]
            {
                "#   #",
                " # # ",
                "  #  ",
                "  #  ",
                "  #  "
            }},
            { 'Z', new string[]
            {
                "#####",
                "   # ",
                "  #  ",
                " #   ",
                "#####"
            }},
            #endregion

            #region Numbers
            { '0', new string[]
            {
                "#####",
                "#  ##",
                "# # #",
                "##  #",
                "#####"
            }},
            { '1', new string[]
            {
                "  #  ",
                " ##  ",
                "  #  ",
                "  #  ",
                "#####"
            }},
            { '2', new string[]
            {
                "#####",
                "    #",
                "#####",
                "#    ",
                "#####"
            }},
            { '3', new string[]
            {
                "#### ",
                "    #",
                "#### ",
                "    #",
                "#### "
            }},
            { '4', new string[]
            {
                "#   #",
                "#   #",
                "#### ",
                "    #",
                "    #"
            }},
            { '5', new string[]
            {
                "#### ",
                "#    ",
                "#### ",
                "    #",
                "#### "
            }},
            { '6', new string[]
            {
                "#### ",
                "#    ",
                "#### ",
                "#   #",
                "#### "
            }},
            { '7', new string[]
            {
                "#####",
                "   # ",
                "  #  ",
                " #   ",
                "#    "
            }},
            { '8', new string[]
            {
                " ### ",
                "#   #",
                " ### ",
                "#   #",
                " ### "
            }},
            { '9', new string[]
            {
                "#### ",
                "#   #",
                "#### ",
                "    #",
                "#### "
            }},
            #endregion

            #region Special Characters
            { ' ', new string[]
            {
                "     ",
                "     ",
                "     ",
                "     ",
                "     "
            }},
            { '!', new string[]
            {
                "  #  ",
                "  #  ",
                "  #  ",
                "     ",
                "  #  "
            }},
            { '"', new string[]
            {
                " # # ",
                " # # ",
                "     ",
                "     ",
                "     "
            }},
            { '#', new string[]
            {
                " # # ",
                "#####",
                " # # ",
                "#####",
                " # # "
            }},
            { '$', new string[]
            {
                " ####",
                "# #  ",
                " ### ",
                "  # #",
                "#####"
            }},
            { '%', new string[]
            {
                " #   #",
                "   #  ",
                "  #   ",
                " #    ",
                "#   # "
            }},
            { '&', new string[]
            {
                " ### ",
                "#   #",
                " ### ",
                "#  # ",
                " ####"
            }},
            { '\'', new string[]
            {
                "  #  ",
                "     ",
                "     ",
                "     ",
                "     "
            }},
            { '(', new string[]
            {
                "  ## ",
                " #   ",
                "#    ",
                " #   ",
                "  ## "
            }},
            { ')', new string[]
            {
                " ##  ",
                "   # ",
                "    #",
                "   # ",
                " ##  "
            }},
            { '*', new string[]
            {
                "  #  ",
                " # # ",
                "#####",
                " # # ",
                "  #  "
            }},
            { '+', new string[]
            {
                "     ",
                "  #  ",
                "#####",
                "  #  ",
                "     "
            }},
            { ',', new string[]
            {
                "     ",
                "     ",
                "     ",
                "  #  ",
                " #   "
            }},
            { '-', new string[]
            {
                "     ",
                "     ",
                "#####",
                "     ",
                "     "
            }},
            { '.', new string[]
            {
                "     ",
                "     ",
                "     ",
                "     ",
                "  #  "
            }},
            { '/', new string[]
            {
                "    #",
                "   # ",
                "  #  ",
                " #   ",
                "#    "
            }},
            { ':', new string[]
            {
                "     ",
                "  #  ",
                "     ",
                "  #  ",
                "     "
            }},
            { ';', new string[]
            {
                "     ",
                "  #  ",
                "     ",
                "  #  ",
                " #   "
            }},
            { '<', new string[]
            {
                "   # ",
                "  #  ",
                " #   ",
                "  #  ",
                "   # "
            }},
            { '=', new string[]
            {
                "     ",
                "#####",
                "     ",
                "#####",
                "     "
            }},
            { '>', new string[]
            {
                " #   ",
                "  #  ",
                "   # ",
                "  #  ",
                " #   "
            }},
            { '?', new string[]
            {
                "#### ",
                "    #",
                "  #  ",
                "     ",
                "  #  "
            }},
            { '[', new string[]
            {
                "#####",
                "#    ",
                "#    ",
                "#    ",
                "#####"
            }},
            { '\\', new string[]
            {
                "#    ",
                " #   ",
                "  #  ",
                "   # ",
                "    #"
            }},
            { ']', new string[]
            {
                "#####",
                "    #",
                "    #",
                "    #",
                "#####"
            }},
            { '^', new string[]
            {
                "  #  ",
                " # # ",
                "     ",
                "     ",
                "     "
            }},
            { '_', new string[]
            {
                "     ",
                "     ",
                "     ",
                "     ",
                "#####"
            }},
            { '`', new string[]
            {
                " #   ",
                "     ",
                "     ",
                "     ",
                "     "
            }},
            { '{', new string[]
            {
                "  ###",
                " #    ",
                "#     ",
                " #    ",
                "  ###"
            }},
            { '|', new string[]
            {
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  ",
                "  #  "
            }},
            { '}', new string[]
            {
                "###  ",
                "   # ",
                "     #",
                "   # ",
                "###  "
            }},
            { '~', new string[]
            {
                "     ",
                " #  #",
                "#  # ",
                "     ",
                "     "
            }}
            #endregion

            /*
            { '@', new string[]
            {
                " #### ",
                "#    #",
                "#  # #",
                "#   # ",
                " #### "
            }},
            */

            // The '@' is sacrificed as a linebreak. Its hardly used and hard to represent.
            // Add other characters here.
        };
        #endregion

        int letterSpacing = 1; // Space between letters.
        int xOffset = 0;       // Offset for each letter.
        int yOffset = 0;       // Y offset to move lines down.

        // Split wordString at "@" to handle line breaks.
        string[] lines = wordString.Split('@');

        foreach (string line in lines)
        {
            foreach (char letter in line.ToUpper())
            {
                if (letterPatterns.TryGetValue(letter, out string[] pattern))
                {
                    for (int y = 0; y < pattern.Length; y++) // Iterate over rows (Y-axis).
                    {
                        for (int x = 0; x < pattern[y].Length; x++) // Iterate over columns (X-axis).
                        {
                            if (pattern[y][x] == '#') // If it's a block.
                            {
                                // Convert to 3D coordinate (flipping axes if needed).
                                int adjustedX = flipAxes ? pattern[y].Length - 1 - x : x; // Flip X if needed.
                                int adjustedY = flipAxes ? pattern.Length - 1 - y : pattern.Length - 1 - y; // Flip Y if needed.

                                // Rotate letters 90.
                                // (adjustedY, adjustedX) = (adjustedX, adjustedY);

                                // Apply 90-degree rotation (swap X and Z).
                                if (rotate90)
                                {
                                    wordBlocks.Add(Vector3.Add(pos, new Vector3(0, adjustedY + yOffset, adjustedX + xOffset))); // Swap X and Z.
                                }
                                else
                                {
                                    wordBlocks.Add(Vector3.Add(pos, new Vector3(adjustedX + xOffset, adjustedY + yOffset, 0)));
                                }
                            }
                        }
                    }
                    xOffset += 5 + letterSpacing; // Move X position for next letter.
                }
            }

            yOffset -= 5; // Move Y position down for next line.
            xOffset = 0;  // Reset X offset for new line.
        }

        return wordBlocks;
    }
    #endregion

    #region Hollow

    public static HashSet<Vector3> HollowObject(Region region, int thickness)
    {
        HashSet<Vector3> regionBlocks = new HashSet<Vector3>();

        int minY = Math.Min((int)region.Position1.Y, (int)region.Position2.Y);
        int maxY = Math.Max((int)region.Position1.Y, (int)region.Position2.Y) + 1;

        int minX = Math.Min((int)region.Position1.X, (int)region.Position2.X);
        int maxX = Math.Max((int)region.Position1.X, (int)region.Position2.X) + 1;

        int minZ = Math.Min((int)region.Position1.Z, (int)region.Position2.Z);
        int maxZ = Math.Max((int)region.Position1.Z, (int)region.Position2.Z) + 1;

        // Iterate over the range of blocks to wrap.
        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    Vector3 currentPosition = new Vector3(x, y, z);

                    // Check if the current block is within the thickness range.
                    bool isOuterLayer = false;

                    // Check all six directions up to the specified thickness.
                    for (int dx = -thickness; dx <= thickness; dx++)
                    {
                        for (int dy = -thickness; dy <= thickness; dy++)
                        {
                            for (int dz = -thickness; dz <= thickness; dz++)
                            {
                                if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) > thickness)
                                    continue; // Only check within the given thickness

                                Vector3 neighbor = new Vector3(x + dx, y + dy, z + dz);

                                if (GetBlockFromLocation(neighbor) == AirID ||
                                    !IsWithinRegion(neighbor, region.Position1, region.Position2))
                                {
                                    isOuterLayer = true;
                                    break;
                                }
                            }
                            if (isOuterLayer) break;
                        }
                        if (isOuterLayer) break;
                    }

                    // If it's not part of the outer layer, hollow it out.
                    if (!isOuterLayer)
                    {
                        regionBlocks.Add(currentPosition);
                    }
                }
            }
        }

        return regionBlocks;
    }
    #endregion

    #region Fill

    public static HashSet<Vector3> FillHollowObject(Region region)
    {
        // This set will contain all the positions we end up filling.
        HashSet<Vector3> filledPositions = new HashSet<Vector3>();

        // Get the min and max bounds of the region (we add 1 to max so that our loops are inclusive of the full region).
        int minX = Math.Min((int)region.Position1.X, (int)region.Position2.X);
        int maxX = Math.Max((int)region.Position1.X, (int)region.Position2.X) + 1;
        int minY = Math.Min((int)region.Position1.Y, (int)region.Position2.Y);
        int maxY = Math.Max((int)region.Position1.Y, (int)region.Position2.Y) + 1;
        int minZ = Math.Min((int)region.Position1.Z, (int)region.Position2.Z);
        int maxZ = Math.Max((int)region.Position1.Z, (int)region.Position2.Z) + 1;

        // Flood-fill the exterior: any air block reachable from the boundaries is "outside" the closed object.
        HashSet<Vector3> exteriorAir = new HashSet<Vector3>();
        Queue<Vector3> queue = new Queue<Vector3>();

        // Helper to add a position to the queue if it's air and not already added.
        void TryEnqueue(Vector3 pos)
        {
            if (!exteriorAir.Contains(pos) && GetBlockFromLocation(pos) == AirID)
            {
                exteriorAir.Add(pos);
                queue.Enqueue(pos);
            }
        }

        // Add all boundary positions (all positions on the faces of the region) to the queue.
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                TryEnqueue(new Vector3(x, y, minZ));
                TryEnqueue(new Vector3(x, y, maxZ - 1));
            }
        }
        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                TryEnqueue(new Vector3(x, minY, z));
                TryEnqueue(new Vector3(x, maxY - 1, z));
            }
        }
        for (int y = minY; y < maxY; y++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                TryEnqueue(new Vector3(minX, y, z));
                TryEnqueue(new Vector3(maxX - 1, y, z));
            }
        }

        // Define the 6 cardinal directions for flood fill.
        Vector3[] directions = new Vector3[]
        {
        new Vector3(-1, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, -1, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, -1),
        new Vector3(0, 0, 1)
        };

        // Flood-fill all exterior air blocks.
        while (queue.Count > 0)
        {
            Vector3 current = queue.Dequeue();
            foreach (var dir in directions)
            {
                Vector3 neighbor = current + dir;
                // Ensure neighbor is within the region bounds.
                if (neighbor.X < minX || neighbor.X >= maxX ||
                    neighbor.Y < minY || neighbor.Y >= maxY ||
                    neighbor.Z < minZ || neighbor.Z >= maxZ)
                {
                    continue;
                }

                TryEnqueue(neighbor);
            }
        }

        // Now, any air block within the region that is NOT in exteriorAir is "inside" the closed object.
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    if (GetBlockFromLocation(pos) == AirID && !exteriorAir.Contains(pos))
                    {
                        // Add the interior blocks for filling to the hashset.
                        filledPositions.Add(pos);
                    }
                }
            }
        }

        return filledPositions;
    }
    #endregion

    #region Flood Fill

    public static HashSet<Vector3> FloodFill(Vector3 start, int maxBlocks = 10000) // Use 10k blocks as the default max.
    {
        HashSet<Vector3> filledPositions = new HashSet<Vector3>();
        Queue<Vector3> queue = new Queue<Vector3>();

        int blockID = GetBlockFromLocation(start);
        if (blockID == AirID) return filledPositions; // Avoid filling air.

        queue.Enqueue(start);
        filledPositions.Add(start);

        // Define 6 cardinal directions for flood fill.
        Vector3[] directions = new Vector3[]
        {
        new Vector3(-1, 0, 0), new Vector3(1, 0, 0),
        new Vector3(0, -1, 0), new Vector3(0, 1, 0),
        new Vector3(0, 0, -1), new Vector3(0, 0, 1)
        };

        while (queue.Count > 0 && filledPositions.Count < maxBlocks)
        {
            Vector3 current = queue.Dequeue();

            foreach (var dir in directions)
            {
                Vector3 neighbor = current + dir;

                // Enforce Y constraints (only flood-fill within Y: max height to min height).
                if (neighbor.Y > WorldHeights.Item1 || neighbor.Y < WorldHeights.Item2) continue;

                // Ensure block matches the block ID and hasn't been visited
                if (GetBlockFromLocation(neighbor) == blockID && !filledPositions.Contains(neighbor))
                {
                    filledPositions.Add(neighbor);
                    queue.Enqueue(neighbor);

                    if (filledPositions.Count >= maxBlocks) break;
                }
            }
        }

        return filledPositions;
    }
    #endregion

    #region Wrap

    public static HashSet<Vector3> WrapObject(Region region, List<int> replaceBlockPattern, bool excludeSurface)
    {
        HashSet<Vector3> regionBlocks = new HashSet<Vector3>();

        int minY = Math.Min((int)region.Position1.Y, (int)region.Position2.Y);
        int maxY = Math.Max((int)region.Position1.Y, (int)region.Position2.Y) + 1;

        int minX = Math.Min((int)region.Position1.X, (int)region.Position2.X);
        int maxX = Math.Max((int)region.Position1.X, (int)region.Position2.X) + 1;

        int minZ = Math.Min((int)region.Position1.Z, (int)region.Position2.Z);
        int maxZ = Math.Max((int)region.Position1.Z, (int)region.Position2.Z) + 1;

        // Iterate over the range of blocks to wrap.
        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    Vector3 currentPosition = new Vector3(x, y, z);

                    // Check if the current block is different from the target block type.
                    bool wrapBlocks = false;
                    if (GetBlockFromLocation(currentPosition) != AirID)
                    {
                        wrapBlocks = !replaceBlockPattern.Contains(GetBlockFromLocation(currentPosition));
                    }

                    if (wrapBlocks)
                    {
                        // Try wrapping the block in 7 possible adjacent positions.
                        for (int direction = 0; direction < 7; direction++)
                        {
                            switch (direction)
                            {
                                case 0:
                                    currentPosition = new Vector3(x, y, z);
                                    break;
                                case 1:
                                    if (!excludeSurface)
                                        currentPosition = new Vector3(x, y + 1f, z);
                                    break;
                                case 2:
                                    currentPosition = new Vector3(x, y - 1f, z);
                                    break;
                                case 3:
                                    currentPosition = new Vector3(x + 1f, y, z);
                                    break;
                                case 4:
                                    currentPosition = new Vector3(x - 1f, y, z);
                                    break;
                                case 5:
                                    currentPosition = new Vector3(x, y, z + 1f);
                                    break;
                                case 6:
                                    currentPosition = new Vector3(x, y, z - 1f);
                                    break;
                            }

                            // If the block is empty, wrap it with the new block type.
                            if (GetBlockFromLocation(currentPosition) == AirID)
                            {
                                regionBlocks.Add(currentPosition);
                            }
                        }
                    }
                }
            }
        }

        return regionBlocks;
    }
    #endregion

    #region Matrix

    public static HashSet<Tuple<Vector3, int>> MakeMatrix(Vector3 pos, int radius, int spacing, bool enableSnow, string optionalBlockPattern)
    {
        HashSet<Tuple<Vector3, int>> maxtrixBlocks = new HashSet<Tuple<Vector3, int>>();

        // Calculate the spacing based on the clipboards size.
        int width = Convert.ToInt32(copiedRegion.Max(t => t.Item1.X) - copiedRegion.Min(t => t.Item1.X) + 1);
        int length = Convert.ToInt32(copiedRegion.Max(t => t.Item1.Z) - copiedRegion.Min(t => t.Item1.Z) + 1);
        int maxFace = Math.Max(width, length);
        spacing += maxFace;

        // Calculate the grid size based on the radius and spacing.
        int startX = Convert.ToInt32(pos.X) - (radius * 3) - spacing + 1;
        int endX = (radius * 3) + Convert.ToInt32(pos.X) + 1;
        int startZ = Convert.ToInt32(pos.Z) - (radius * 3) - spacing + 1;
        int endZ = (radius * 3) + Convert.ToInt32(pos.Z) + 1;

        for (int x = startX; x < endX; x += spacing)
        {
            for (int z = startZ; z < endZ; z += spacing)
            {
                foreach (var blockData in copiedRegion)
                {
                    // Parse block data.
                    int offsetX = (int)blockData.Item1.X;
                    int offsetY = (int)blockData.Item1.Y;
                    int offsetZ = (int)blockData.Item1.Z;
                    int blockId = blockData.Item2;

                    // Adjust Y position based on terrain height if snow is enabled.
                    int finalY = enableSnow ? GetTerrainHeight(x + offsetX, z + offsetZ) : Convert.ToInt32(pos.Y) + offsetY;

                    // Calculate the final position.
                    Vector3 blockPosition = new Vector3(x + offsetX, finalY, z + offsetZ);

                    // Get the block id.
                    if (!string.IsNullOrEmpty(optionalBlockPattern))
                    {
                        // Overwrite block id value with a specified value.
                        blockId = GetRandomBlockFromPattern(optionalBlockPattern);
                    }

                    // Spawn the block at the calculated position.
                    maxtrixBlocks.Add(new Tuple<Vector3, int>(blockPosition, blockId));
                }
            }
        }

        return maxtrixBlocks;
    }
    private static int GetTerrainHeight(int x, int z)
    {
        // Loop through each Y-level from 62 to -62, checking for non-empty blocks (e.g., terrain).
        for (int y = WorldHeights.Item1; y > (WorldHeights.Item2 - 1); y--)
        {
            // Create a vector for the world coordinates at each y-level.
            Vector3 currentPosition = new Vector3(x, y, z);

            // Check if the block is not empty.
            if (GetBlockFromLocation(currentPosition) != AirID)
            {
                // If it's not empty, set the base height of the tree.
                return (y + 1);
            }
        }
        return WorldHeights.Item2; // Return the lowest world level.
    }
    #endregion

    #region Snow

    public static HashSet<Vector3> MakeSnow(Vector3 center, int radius, bool replaceSurface = false)
    {
        HashSet<Vector3> snowBlocks = new HashSet<Vector3>();

        for (int x = (int)center.X - radius; x <= (int)center.X + radius; ++x)
        {
            for (int z = (int)center.Z - radius; z <= (int)center.Z + radius; ++z)
            {
                // Ensure the point is within the circular radius.
                if ((x - (int)center.X) * (x - (int)center.X) + (z - (int)center.Z) * (z - (int)center.Z) <= radius * radius)
                {
                    // Loop through each Y-level from 62 to -62, checking for non-empty blocks (e.g., terrain).
                    for (int y = WorldHeights.Item1; y > WorldHeights.Item2 - 1; y--)
                    {
                        // Check if the block is not empty.
                        if (GetBlockFromLocation(new Vector3(x, y, z)) != AirID)
                        {
                            // If replace mode is enabled, don't offset upwards.
                            if (replaceSurface)
                                snowBlocks.Add(new Vector3(x, y, z));
                            else
                                snowBlocks.Add(new Vector3(x, y + 1f, z));
                            break;
                        }
                    }
                }
            }
        }

        return snowBlocks;
    }
    #endregion

    #region Forest

    public static XnaPerlinNoise _noiseFunction;
    public static HashSet<Tuple<Vector3, int>> MakeForest(Region pos, int density, int max_height)
    {
        HashSet<Tuple<Vector3, int>> forestBlocks = new HashSet<Tuple<Vector3, int>>();

        for (int i = 0; i < density; i++)
        {
            // Generate random positions within the given radius size.
            // float RandomX = (float)(GenerateRandomNumber(0, radius_size * 2) - radius_size) + (float)pos.X;
            // float RandomZ = (float)(GenerateRandomNumber(0, radius_size * 2) - radius_size) + (float)pos.Z;

            // Generate random positions within the given region.
            float RandomX = (float)GenerateRandomNumber((int)pos.Position1.X, (int)pos.Position2.X);
            float RandomZ = (float)GenerateRandomNumber((int)pos.Position1.Z, (int)pos.Position2.Z);

            // Merge the new tree blocks into the forestBlocks set.
            forestBlocks.UnionWith(MakeTree((int)RandomX, (int)RandomZ, max_height));
        }

        return forestBlocks;
    }
    #endregion

    #region Tree

    public static HashSet<Tuple<Vector3, int>> MakeTree(int worldX, int worldZ, int maxHeight)
    {
        _noiseFunction = new XnaPerlinNoise(new Random(GenerateRandomNumber(int.MinValue, int.MaxValue)));
        HashSet<Tuple<Vector3, int>> treeBlocks = new HashSet<Tuple<Vector3, int>>();

        try
        {
            // Start by assuming a height below the surface.
            float treeBaseHeight = WorldHeights.Item2;

            // Loop through each Y-level from 62 to -62, checking for non-empty blocks (e.g., terrain).
            for (int y = WorldHeights.Item1; y > (WorldHeights.Item2 - 1); y--)
            {
                // Create a vector for the world coordinates at each y-level.
                Vector3 currentPosition = new Vector3(worldX, y, worldZ);

                // Check if the block is not empty.
                if (GetBlockFromLocation(currentPosition) != AirID)
                {
                    // If it's not empty, set the base height of the tree.
                    treeBaseHeight = y + 1f;
                    break;
                }
            }

            // Set the final position where the tree should start.
            Vector3 treeTopPosition = new Vector3(worldX, (int)treeBaseHeight - 1, worldZ);

            // If the top block is not leaves, start building the tree.
            if (GetBlockFromLocation(treeTopPosition) != LeavesID)
            {
                int trunkHeight = 0;
                // Randomly select the height of the tree between 4 and maxHeight.
                int treeHeight = GenerateRandomNumber(4, maxHeight);

                // Build the trunk of the tree by placing log blocks.
                for (int i = 0; i < treeHeight; i++)
                {
                    if ((int)treeBaseHeight + trunkHeight <= WorldHeights.Item1)
                    {
                        Vector3 trunkPosition = new Vector3(worldX, (int)treeBaseHeight + trunkHeight, worldZ);

                        // Add block to the builder.
                        treeBlocks.Add(new Tuple<Vector3, int>(trunkPosition, LogID));
                    }
                    trunkHeight++;
                }

                // Generate foliage around the tree.
                for (int xOffset = -3; xOffset <= 3; xOffset++)
                {
                    for (int yOffset = -3; yOffset <= 3; yOffset++)
                    {
                        for (int zOffset = -3; zOffset <= 3; zOffset++)
                        {
                            // Create a position vector for foliage placement.
                            Vector3 foliagePosition = new Vector3(worldX + xOffset, (int)treeBaseHeight + yOffset + trunkHeight, worldZ + zOffset);

                            // Ensure the foliage is within the map height limit.
                            if ((float)foliagePosition.Y <= WorldHeights.Item1)
                            {
                                // Check if the block is empty or unassigned
                                int blockType = GetBlockFromLocation(foliagePosition);
                                if (blockType == 0 || blockType == 94) // Empty or NumberOfBlocks (null).
                                {
                                    // Generate noise and distance-based checks to place foliage (leaves).
                                    float noiseValue = _noiseFunction.ComputeNoise(foliagePosition * 0.5f);
                                    float distanceValue = 1f - (float)Math.Sqrt((xOffset * xOffset + yOffset * yOffset + zOffset * zOffset)) / 3f;

                                    // If the noise + distance exceeds a threshold, place leaves.
                                    if (noiseValue + distanceValue > 0.25f)
                                    {
                                        // Add block to the builder.
                                        treeBlocks.Add(new Tuple<Vector3, int>(foliagePosition, LeavesID));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return treeBlocks;
        }
        catch (Exception)
        {
            return treeBlocks;
        }
    }
    #endregion

    #endregion

    /// <summary>
    /// 
    /// 'Floor'
    /// 'Cube'
    /// 'Sphere'
    /// 'Pyramid'
    /// 'Cone'
    /// 'Cylinder'
    /// 'Diamond'
    /// 'Ring'
    /// 
    /// </summary>
    #region Generation Methods

    #region Floor

    public static HashSet<Vector3> MakeFloor(Vector3 pos, int radius, bool hollow, int ignoreBlock = -1)
    {
        HashSet<Vector3> floorBlocks = new HashSet<Vector3>();

        int halfradius = radius / 2; // Calculate half the radius for centering.

        for (int x = -halfradius; x < radius - halfradius; ++x)
        {
            for (int z = -halfradius; z < radius - halfradius; ++z)
            {
                if (!hollow || x == -halfradius || x == radius - halfradius - 1 ||
                               z == -halfradius || z == radius - halfradius - 1)
                {
                    // If ignore block was specified, skip locations matching the block id.
                    Vector3 newPos = Vector3.Add(pos, new Vector3(x, 0, z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        floorBlocks.Add(newPos);
                }
            }
        }

        return floorBlocks;
    }
    #endregion

    #region Cube

    public static HashSet<Vector3> MakeCube(Vector3 pos, int radii, bool hollow, int ignoreBlock = -1)
    {
        HashSet<Vector3> cubeBlocks = new HashSet<Vector3>();

        int halfradii = radii / 2; // Calculate half the radii for centering.

        for (int x = -halfradii; x < radii - halfradii; ++x)
        {
            for (int y = 0; y < radii; ++y)
            {
                // Ensure Y is within the world's height constraints.
                int worldY = (int)pos.Y + y; // Calculate actual world Y position.
                if (worldY > WorldHeights.Item1 || worldY < WorldHeights.Item2)
                    continue;

                for (int z = -halfradii; z < radii - halfradii; ++z)
                {
                    if (!hollow || x == -halfradii || x == radii - halfradii - 1 ||
                                   y == 0 || y == radii - 1 ||
                                   z == -halfradii || z == radii - halfradii - 1)
                    {
                        // If ignore block was specified, skip locations matching the block id.
                        Vector3 newPos = Vector3.Add(pos, new Vector3(x, y, z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            cubeBlocks.Add(newPos);
                    }
                }
            }
        }

        return cubeBlocks;
    }
    #endregion

    #region Sphere

    public static HashSet<Vector3> MakeSphere(Vector3 pos, double radiusX, double radiusY, double radiusZ, bool hollow, int ignoreBlock = -1)
    {
        HashSet<Vector3> sphereBlocks = new HashSet<Vector3>();

        radiusX += 0.5;
        radiusY += 0.5;
        radiusZ += 0.5;

        double invRadiusX = 1 / radiusX;
        double invRadiusY = 1 / radiusY;
        double invRadiusZ = 1 / radiusZ;

        int ceilRadiusX = (int)Math.Ceiling(radiusX);
        int ceilRadiusY = (int)Math.Ceiling(radiusY);
        int ceilRadiusZ = (int)Math.Ceiling(radiusZ);

        // Shift pos.Y up so the sphere is fully above the original Y position.
        pos.Y += (float)radiusY;

        double nextXn = 0;
        for (int x = 0; x <= ceilRadiusX; ++x)
        {
            double xn = nextXn;
            nextXn = (x + 1) * invRadiusX;
            double nextYn = 0;
            for (int y = 0; y <= ceilRadiusY; ++y)
            {
                double yn = nextYn;
                nextYn = (y + 1) * invRadiusY;
                double nextZn = 0;

                for (int z = 0; z <= ceilRadiusZ; ++z)
                {
                    double zn = nextZn;
                    nextZn = (z + 1) * invRadiusZ;
                    double distanceSq = LengthSq(xn, yn, zn);
                    if (distanceSq > 1)
                    {
                        if (z == 0)
                        {
                            if (y == 0)
                            {
                                break;
                            }

                            break;
                        }

                        break;
                    }

                    if (hollow)
                    {
                        if (LengthSq(nextXn, yn, zn) <= 1 && LengthSq(xn, nextYn, zn) <= 1 && LengthSq(xn, yn, nextZn) <= 1)
                        {
                            continue;
                        }
                    }

                    // Ensure Y is within the world's height constraints.
                    // If ignore block was specified, skip locations matching the block id.
                    Vector3 newPos = Vector3.Add(pos, new Vector3(x, y, z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(-x, y, z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(x, -y, z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(x, y, -z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(-x, -y, z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(x, -y, -z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(-x, y, -z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(-x, -y, -z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        if (IsWithinWorldBounds(newPos, 1, 1)) sphereBlocks.Add(newPos);
                }
            }
        }

        return sphereBlocks;
    }
    #endregion

    #region Pyramid

    public static HashSet<Vector3> MakePyramid(Vector3 pos, int size, bool hollow, int ignoreBlock = -1)
    {
        HashSet<Vector3> pyramidBlocks = new HashSet<Vector3>();

        int height = size;
        for (int y = 0; y <= height; ++y)
        {
            // Ensure Y is within the world's height constraints.
            if (!IsWithinWorldBounds((int)(pos.Y + y))) continue;

            size--;
            for (int x = 0; x <= size; ++x)
            {
                for (int z = 0; z <= size; ++z)
                {
                    if ((!hollow && z <= size && x <= size) || z == size || x == size)
                    {
                        // If ignore block was specified, skip locations matching the block id.
                        Vector3 newPos = Vector3.Add(pos, new Vector3(x, y, z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            pyramidBlocks.Add(newPos);

                        newPos = Vector3.Add(pos, new Vector3(-x, y, z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            pyramidBlocks.Add(newPos);

                        newPos = Vector3.Add(pos, new Vector3(x, y, -z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            pyramidBlocks.Add(newPos);

                        newPos = Vector3.Add(pos, new Vector3(-x, y, -z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            pyramidBlocks.Add(newPos);
                    }
                }
            }
        }

        return pyramidBlocks;
    }
    #endregion

    #region Cone

    public static HashSet<Vector3> MakeCone(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, double thickness, int ignoreBlock = -1)
    {
        HashSet<Vector3> coneBlocks = new HashSet<Vector3>();

        int ceilRadiusX = (int)Math.Ceiling(radiusX);
        int ceilRadiusZ = (int)Math.Ceiling(radiusZ);

        double radiusXPow = Math.Pow(radiusX, 2);
        double radiusZPow = Math.Pow(radiusZ, 2);
        double heightPow = Math.Pow(height, 2);

        for (int y = 0; y < height; ++y)
        {
            // Ensure Y is within the world's height constraints.
            if (!IsWithinWorldBounds((int)(pos.Y + y))) continue;

            double ySquaredMinusHeightOverHeightSquared = Math.Pow(y - height, 2) / heightPow;
            for (int x = 0; x <= ceilRadiusX; ++x)
            {
                double xSquaredOverRadiusX = Math.Pow(x, 2) / radiusXPow;
                for (int z = 0; z <= ceilRadiusZ; ++z)
                {
                    double zSquaredOverRadiusZ = Math.Pow(z, 2) / radiusZPow;
                    double distanceFromOriginMinusHeightSquared = xSquaredOverRadiusX + zSquaredOverRadiusZ - ySquaredMinusHeightOverHeightSquared;
                    if (distanceFromOriginMinusHeightSquared > 1)
                    {
                        if (z == 0)
                        {
                            break;
                        }

                        break;
                    }

                    if (hollow)
                    {
                        double xNext = Math.Pow(x + thickness, 2) / radiusXPow + zSquaredOverRadiusZ - ySquaredMinusHeightOverHeightSquared;
                        double yNext = xSquaredOverRadiusX + zSquaredOverRadiusZ - Math.Pow(y + thickness - height, 2) / heightPow;
                        double zNext = xSquaredOverRadiusX + Math.Pow(z + thickness, 2) / radiusZPow - ySquaredMinusHeightOverHeightSquared;
                        if (xNext <= 0 && zNext <= 0 && (yNext <= 0 && y + thickness != height))
                        {
                            continue;
                        }
                    }

                    if (distanceFromOriginMinusHeightSquared <= 0)
                    {
                        // If ignore block was specified, skip locations matching the block id.
                        Vector3 newPos = Vector3.Add(pos, new Vector3(x, y, z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            coneBlocks.Add(newPos);

                        newPos = Vector3.Add(pos, new Vector3(-x, y, z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            coneBlocks.Add(newPos);

                        newPos = Vector3.Add(pos, new Vector3(x, y, -z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            coneBlocks.Add(newPos);

                        newPos = Vector3.Add(pos, new Vector3(-x, y, -z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            coneBlocks.Add(newPos);
                    }
                }
            }
        }

        return coneBlocks;
    }
    #endregion

    #region Cylinder

    public static HashSet<Vector3> MakeCylinder(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, int ignoreBlock = -1)
    {
        HashSet<Vector3> cylinderBlocks = new HashSet<Vector3>();

        radiusX += 0.5;
        radiusZ += 0.5;

        if (height == 0)
        {
            return cylinderBlocks;
        }
        else if (height < 0)
        {
            height = -height;
            pos = Vector3.Subtract(pos, new Vector3(0, height, 0));
        }

        // Assuming world, getMinY, and getMaxY are accessible from your context
        // if (pos.Y < world.Y)
        // {
        //     pos.Y = world.Y;
        // }
        // else if (pos.Y + height - 1 > world.Y)
        // {
        //     height = (int)(world.Y - pos.Y + 1);
        // }

        double invRadiusX = 1 / radiusX;
        double invRadiusZ = 1 / radiusZ;

        int ceilRadiusX = (int)Math.Ceiling(radiusX);
        int ceilRadiusZ = (int)Math.Ceiling(radiusZ);

        double nextXn = 0;
        for (int x = 0; x <= ceilRadiusX; ++x)
        {
            double xn = nextXn;
            nextXn = (x + 1) * invRadiusX;
            double nextZn = 0;
            for (int z = 0; z <= ceilRadiusZ; ++z)
            {
                double zn = nextZn;
                nextZn = (z + 1) * invRadiusZ;
                double distanceSq = LengthSq(xn, zn);
                if (distanceSq > 1)
                {
                    if (z == 0)
                    {
                        break;
                    }

                    break;
                }

                if (hollow)
                {
                    if (LengthSq(nextXn, zn) <= 1 && LengthSq(xn, nextZn) <= 1)
                    {
                        continue;
                    }
                }

                for (int y = 0; y < height; ++y)
                {
                    // Ensure Y is within the world's height constraints.
                    if (!IsWithinWorldBounds((int)(pos.Y + y))) continue;
                    
                    // If ignore block was specified, skip locations matching the block id.
                    Vector3 newPos = Vector3.Add(pos, new Vector3(x, y, z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        cylinderBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(-x, y, z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        cylinderBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(x, y, -z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        cylinderBlocks.Add(newPos);

                    newPos = Vector3.Add(pos, new Vector3(-x, y, -z));
                    if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                        cylinderBlocks.Add(newPos);
                }
            }
        }

        return cylinderBlocks;
    }
    #endregion

    #region Diamond

    public static HashSet<Vector3> MakeDiamond(Vector3 pos, int size, bool hollow, bool squared, int ignoreBlock = -1)
    {
        HashSet<Vector3> diamondBlocks = new HashSet<Vector3>();
        int halfSize = size / 2;

        // Loop through Y levels to form the full diamond shape.
        // SPAWN_CENETER: for (int y = -halfSize; y <= halfSize; y++)
        for (int y = 0; y <= size; y++)
        {
            // Ensure Y is within the world's height constraints.
            if (!IsWithinWorldBounds((int)(pos.Y + y))) continue;

            int layerSize = halfSize - Math.Abs(y - halfSize); // Determine how wide or round each layer is. // Use  '- halfSize' for above pos.Y.
            int squaredRadius = layerSize * layerSize;
            bool isTopOrBottom = (y == 0 || y == size);
            // SPAWN_CENETER: bool isTopOrBottom = (y == -halfSize || y == halfSize);

            for (int x = -layerSize; x <= layerSize; x++)
            {
                for (int z = -layerSize; z <= layerSize; z++)
                {
                    bool insideShape = squared || (x * x + z * z <= squaredRadius);
                    bool isBorder = squared ? (Math.Abs(x) == layerSize || Math.Abs(z) == layerSize) : (x * x + z * z >= (layerSize - 1) * (layerSize - 1));

                    if (insideShape && (!hollow || isBorder || isTopOrBottom))
                    {
                        // If ignore block was specified, skip locations matching the block id.
                        Vector3 newPos = Vector3.Add(pos, new Vector3(x, y, z));
                        if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                            diamondBlocks.Add(newPos);
                    }
                }
            }
        }

        return diamondBlocks;
    }
    #endregion

    #region Ring

    public static HashSet<Vector3> MakeRing(Vector3 pos, double radius, bool hollow, int ignoreBlock = -1)
    {
        HashSet<Vector3> ringBlocks = new HashSet<Vector3>();

        radius += 0.5; // Adjust radius for correct block placement.

        double invRadius = 1 / radius;
        int ceilRadius = (int)Math.Ceiling(radius);

        double nextXn = 0;
        for (int x = 0; x <= ceilRadius; ++x)
        {
            double xn = nextXn;
            nextXn = (x + 1) * invRadius;
            double nextZn = 0;

            for (int z = 0; z <= ceilRadius; ++z)
            {
                double zn = nextZn;
                nextZn = (z + 1) * invRadius;

                double distanceSq = LengthSq(xn, 0, zn); // Y is always 0.
                if (distanceSq > 1)
                {
                    if (z == 0) break;
                    break;
                }

                if (hollow)
                {
                    if (LengthSq(nextXn, 0, zn) <= 1 && LengthSq(xn, 0, nextZn) <= 1)
                    {
                        continue;
                    }
                }

                // If ignore block was specified, skip locations matching the block id.
                // Place blocks only on the Y = pos.Y plane.
                Vector3 newPos = Vector3.Add(pos, new Vector3(x, 0, z));
                if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                    ringBlocks.Add(newPos);

                newPos = Vector3.Add(pos, new Vector3(-x, 0, z));
                if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                    ringBlocks.Add(newPos);

                newPos = Vector3.Add(pos, new Vector3(x, 0, -z));
                if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                    ringBlocks.Add(newPos);

                newPos = Vector3.Add(pos, new Vector3(-x, 0, -z));
                if (ignoreBlock == -1 || GetBlockFromLocation(newPos) != ignoreBlock)
                    ringBlocks.Add(newPos);
            }
        }

        return ringBlocks;
    }
    #endregion

    #endregion

    /// <summary>
    /// 
    /// 'Schematics'
    /// 'Copy'
    /// 'Cut'
    /// 'Paste'
    /// 'Rotate'
    /// 'Flip'
    /// 'ClearClipboard'
    /// 
    /// </summary>
    #region Schematic and Clipboard Methods

    #region Schematics

    private static readonly byte WE_SAVE_VERSION = 0x1;
    private static readonly byte[] WE_SAVE_HEADER = Encoding.UTF8.GetBytes("WES");
    public static void SaveSchematic(HashSet<Tuple<Vector3, int>> clipboard, FileInfo schemPath, bool overwriteFile = false, bool saveAir = false)
    {
        if (clipboard.Count() <= 0)
        {
            Console.WriteLine("Your clipboard is empty.");
            return;
        }

        if (schemPath.Directory != null && !schemPath.Directory.Exists)
            schemPath.Directory.Create();

        if (schemPath.Exists)
        {
            if (overwriteFile)
                File.Delete(schemPath.FullName); // Delete the file and overwrite it.
            else
            {
                Console.WriteLine("A save with that name already exists.");
                return;
            }
        }

        long len = 0;
        using (FileStream stream = schemPath.Create())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(WE_SAVE_HEADER);
            writer.Write(WE_SAVE_VERSION);

            // Serialize the data manually using BinaryWriter.
            foreach (var item in clipboard)
            {
                // Check to save air. If not, skip saving air blocks.
                if (!saveAir && item.Item2 == AirID) continue;
                
                writer.Write(item.Item1.X);
                writer.Write(item.Item1.Y);
                writer.Write(item.Item1.Z);
                writer.Write(item.Item2);
            }

            len = stream.Length;
            stream.Flush();
        }

        Console.WriteLine($"Created save called {schemPath.Name} successfully. (size is {len / 1024} KB)");
    }

    public static void LoadSchematic(FileInfo schemPath, bool loadAir = false)
    {
        if (!schemPath.Exists)
        {
            Console.WriteLine("Save does not exist.");
            return;
        }

        using (FileStream stream = schemPath.OpenRead())
        using (BinaryReader reader = new BinaryReader(stream))
        {
            byte[] header = reader.ReadBytes(3);
            if (!UnsafeCompare(header, WE_SAVE_HEADER))
            {
                Console.WriteLine("Not a valid save file.");
                return;
            }

            int version = reader.ReadByte();
            if (version != WE_SAVE_VERSION)
            {
                Console.WriteLine($"Invalid save version. (file is version 0x{version:X}, you have 0x{WE_SAVE_VERSION:X})");
                Console.WriteLine($"Your save WILL still work; however, it must be done with a different version.");
                return;
            }

            copiedRegion.Clear(); // Clear existing region.

            // Deserialize data.
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();
                var value = reader.ReadInt32();

                // Check to load air. If not, skip loading air blocks.
                if (!loadAir && value == AirID) continue;

                copiedRegion.Add(new Tuple<Vector3, int>(new Vector3(x, y, z), value));
            }
        }
        Console.WriteLine($"Loaded save {schemPath.Name} from file. ({copiedRegion.Count} blocks)");
    }

    #region Schematic Helper

    // Distributed under the MIT/X11 software license.
    // Ref: http://www.opensource.org/licenses/mit-license.php.
    static unsafe bool UnsafeCompare(byte[] a1, byte[] a2)
    {
        if (a1 == a2) return true;
        if (a1 == null || a2 == null || a1.Length != a2.Length)
            return false;
        fixed (byte* p1 = a1, p2 = a2)
        {
            byte* x1 = p1, x2 = p2;
            int l = a1.Length;
            for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                if (*((long*)x1) != *((long*)x2)) return false;
            if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
            if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
            if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
            return true;
        }
    }
    #endregion

    #endregion

    #region Copy

    public static void CopyRegion(Region region)
    {
        // Clear existing region.
        copiedRegion.Clear();

        // Get the smallest X value.
        int[] xData = { (int)region.Position1.X, (int)region.Position2.X };
        int[] yData = { (int)region.Position1.Y, (int)region.Position2.Y };
        int[] zData = { (int)region.Position1.Z, (int)region.Position2.Z };

        for (int y = (int)region.Position1.Y; y <= (int)region.Position2.Y; ++y)
        {
            for (int x = (int)region.Position1.X; x <= (int)region.Position2.X; ++x)
            {
                for (int z = (int)region.Position1.Z; z <= (int)region.Position2.Z; ++z)
                {
                    // Get block from position.
                    Vector3 redactedLocation = new Vector3(x - xData.Min(), y - yData.Min(), z - zData.Min());

                    // Save copied block from location.
                    copiedRegion.Add(new Tuple<Vector3, int>(redactedLocation, GetBlockFromLocation(new Vector3(x, y, z))));

                    // Console.WriteLine(" {0} : {1}", location, GetBlockFromLocation(location));
                }
            }
        }
    }
    #endregion

    #region Cut

    public static void CutRegion(Region region)
    {
        // Clear existing region.
        copiedRegion.Clear();

        // Get the smallest X value.
        int[] xData = { (int)region.Position1.X, (int)region.Position2.X };
        int[] yData = { (int)region.Position1.Y, (int)region.Position2.Y };
        int[] zData = { (int)region.Position1.Z, (int)region.Position2.Z };

        for (int y = (int)region.Position1.Y; y <= (int)region.Position2.Y; ++y)
        {
            for (int x = (int)region.Position1.X; x <= (int)region.Position2.X; ++x)
            {
                for (int z = (int)region.Position1.Z; z <= (int)region.Position2.Z; ++z)
                {
                    // Get block from position.
                    Vector3 redactedLocation = new Vector3(x - xData.Min(), y - yData.Min(), z - zData.Min());

                    // Save copied block from location.
                    copiedRegion.Add(new Tuple<Vector3, int>(redactedLocation, GetBlockFromLocation(new Vector3(x, y, z))));

                    // Remove all existing valid blocks.
                    if (GetBlockFromLocation(redactedLocation) != AirID)
                        PlaceBlock(redactedLocation, AirID);
                }
            }
        }
    }
    #endregion

    #region Paste

    public static HashSet<Tuple<Vector3, int>> PasteRegion(Vector3 location)
    {
        HashSet<Tuple<Vector3, int>> pasteBlocks = new HashSet<Tuple<Vector3, int>>();

        foreach (var tuple in copiedRegion)
        {
            // Get block location from saved data and add it to the current position.
            Vector3 blockLocation = Vector3.Add(location, tuple.Item1);

            // Get block ID from saved data.
            int block = tuple.Item2;

            // Save copied block from location.
            pasteBlocks.Add(new Tuple<Vector3, int>(blockLocation, block));
        }

        return pasteBlocks;
    }
    #endregion

    #region Rotate

    public static void RotateClipboard(int rotateX, int rotateY, int rotateZ)
    {
        // Find the center of the region.
        Vector3 center = GetRegionCenter();

        var rotatedRegion = new HashSet<Tuple<Vector3, int>>();

        // Variables to track the min and max bounds of the rotated region.
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var item in copiedRegion)
        {
            // Translate the vector to the origin (subtract the center).
            Vector3 translatedVector = item.Item1 - center;

            // Apply the rotation.
            Vector3 rotatedVector = RotateVector(translatedVector, rotateX, rotateY, rotateZ);

            // Translate it back (add the center).
            rotatedVector += center;

            // Update the bounding box for the rotated region.
            minX = Math.Min(minX, rotatedVector.X);
            minY = Math.Min(minY, rotatedVector.Y);
            minZ = Math.Min(minZ, rotatedVector.Z);
            maxX = Math.Max(maxX, rotatedVector.X);
            maxY = Math.Max(maxY, rotatedVector.Y);
            maxZ = Math.Max(maxZ, rotatedVector.Z);

            rotatedRegion.Add(new Tuple<Vector3, int>(rotatedVector, item.Item2));
        }

        // Find the corner of the rotated region (min values).
        Vector3 rotatedCorner = new Vector3(minX, minY, minZ);

        // Calculate the offset to align the corner of the rotated region with the target paste location (0, 0, 0).
        Vector3 offset = -rotatedCorner;

        // Apply the offset to all rotated vectors.
        var alignedRegion = new HashSet<Tuple<Vector3, int>>();
        foreach (var item in rotatedRegion)
        {
            Vector3 alignedVector = item.Item1 + offset;
            alignedRegion.Add(new Tuple<Vector3, int>(alignedVector, item.Item2));
        }

        // Replace the original region with the aligned rotated region.
        copiedRegion = alignedRegion;
    }

    #region Rotational Math Helpers

    public static Vector3 RotateVector(Vector3 vector, int rotateX, int rotateY, int rotateZ)
    {
        // Rotate the vector around the X axis.
        vector = RotateAroundX(vector, rotateX);

        // Rotate the vector around the Y axis.
        vector = RotateAroundY(vector, rotateY);

        // Rotate the vector around the Z axis.
        vector = RotateAroundZ(vector, rotateZ);

        return vector;
    }

    // Rotate around the X axis.
    public static Vector3 RotateAroundX(Vector3 vector, int angle)
    {
        if (angle == 0) return vector;

        float radians = (float)(Math.PI * angle / 180);
        float y = vector.Y;
        float z = vector.Z;
        vector.Y = (float)(y * Math.Cos(radians) - z * Math.Sin(radians));
        vector.Z = (float)(y * Math.Sin(radians) + z * Math.Cos(radians));

        return vector;
    }

    // Rotate around the Y axis.
    public static Vector3 RotateAroundY(Vector3 vector, int angle)
    {
        if (angle == 0) return vector;

        float radians = (float)(Math.PI * angle / 180);
        float x = vector.X;
        float z = vector.Z;
        vector.X = (float)(x * Math.Cos(radians) + z * Math.Sin(radians));
        vector.Z = (float)(-x * Math.Sin(radians) + z * Math.Cos(radians));

        return vector;
    }

    // Rotate around the Z axis.
    public static Vector3 RotateAroundZ(Vector3 vector, int angle)
    {
        if (angle == 0) return vector;

        float radians = (float)(Math.PI * angle / 180);
        float x = vector.X;
        float y = vector.Y;
        vector.X = (float)(x * Math.Cos(radians) - y * Math.Sin(radians));
        vector.Y = (float)(x * Math.Sin(radians) + y * Math.Cos(radians));

        return vector;
    }
    #endregion

    #endregion

    #region Flip

    public static void FlipClipboard(Direction direction)
    {
        // Find the center of the copied region.
        Vector3 center = GetRegionCenter();

        var flippedRegion = new HashSet<Tuple<Vector3, int>>();

        // Variables to track the min and max bounds of the flipped region.
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var item in copiedRegion)
        {
            // Flip the vector.
            Vector3 flippedVector = FlipVector(item.Item1, center, direction);

            // Update the bounding box for the flipped region.
            minX = Math.Min(minX, flippedVector.X);
            minY = Math.Min(minY, flippedVector.Y);
            minZ = Math.Min(minZ, flippedVector.Z);
            maxX = Math.Max(maxX, flippedVector.X);
            maxY = Math.Max(maxY, flippedVector.Y);
            maxZ = Math.Max(maxZ, flippedVector.Z);

            flippedRegion.Add(new Tuple<Vector3, int>(flippedVector, item.Item2));
        }

        // Find the corner of the flipped region (min values).
        Vector3 flippedCorner = new Vector3(minX, minY, minZ);

        // Calculate the offset to align the corner of the flipped region with the target paste location (0, 0, 0).
        Vector3 offset = -flippedCorner;

        // Apply the offset to all flipped vectors.
        var alignedRegion = new HashSet<Tuple<Vector3, int>>();
        foreach (var item in flippedRegion)
        {
            Vector3 alignedVector = item.Item1 + offset;
            alignedRegion.Add(new Tuple<Vector3, int>(alignedVector, item.Item2));
        }

        // Replace the original region with the aligned flipped region.
        copiedRegion = alignedRegion;
    }

    #region Flip Math Helpers

    public static Vector3 FlipVector(Vector3 vector, Vector3 center, Direction direction)
    {
        switch (direction)
        {
            case Direction.posX:
            case Direction.negX:
                return new Vector3(2 * center.X - vector.X, vector.Y, vector.Z);

            case Direction.posZ:
            case Direction.negZ:
                return new Vector3(vector.X, vector.Y, 2 * center.Z - vector.Z);

            case Direction.Up:
            case Direction.Down:
                return new Vector3(vector.X, 2 * center.Y - vector.Y, vector.Z);

            default:
                return vector;
        }
    }
    #endregion

    #endregion

    #region ClearClipboard

    public static void ClearClipboard()
    {
        UndoStack.Clear();
        RedoStack.Clear();
        copiedRegion.Clear();
        copiedStackRegion.Clear();
    }
    #endregion

    #endregion
}