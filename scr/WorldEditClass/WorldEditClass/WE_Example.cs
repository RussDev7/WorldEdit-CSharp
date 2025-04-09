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

using System.Collections.Generic;
using DNA.Drawing.UI.Controls;
using System.Threading.Tasks;
using DNA.CastleMinerZ.Net;
using System.Windows.Forms;
using System.Windows.Input;
using System.Reflection;
using System.Numerics;
using System.Linq;
using System.IO;
using System;

using static WorldEdit.WorldUtils;
using static WorldEdit;

using Vector3 = Microsoft.Xna.Framework.Vector3;               // For testing purposes.
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState; // For testing purposes.
using MouseState = Microsoft.Xna.Framework.Input.MouseState;   // For testing purposes.
using Mouse = Microsoft.Xna.Framework.Input.Mouse;             // For testing purposes.

namespace DNA.CastleMinerZ.UI
{
    public partial class PlainChatInputScreen : UIControlScreen
    {
        #region Variables

        private static Vector3 _pointToLocation1;
        private static Vector3 _pointToLocation2;

        private bool _wandEnabled;
        private bool _toolEnabled;
        private bool _brushEnabled;

        private string _toolCommand = "";
        private int _toolItem = WandItemID;  // Use the wand item as a placeholder.
        private int _brushItem = WandItemID; // Use the wand item as a placeholder.

        #endregion

        #region Text Edit Control

        #pragma warning disable IDE1006 // Suppress naming styles warning.
        private void _textEditControl_EnterPressed(object sender, EventArgs e)
        #pragma warning restore IDE1006
        {
            string inputText = _textEditControl.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputText)) return;

            if (inputText.StartsWith("/"))
            {
                HandleChatCommand(inputText);
            }
            else
            {
                // Not a command, do normal chat functions.
                BroadcastTextMessage.Send(_game.MyNetworkGamer, $"{_game.MyNetworkGamer.Gamertag}: {inputText}");
            }

            _textEditControl.Text = ""; // Clear input to prevent repeated execution.
            base.PopMe();
        }
        #endregion

        #region Chat Command Handler

        private Dictionary<string, Action<string[]>> commandMap;
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public class CommandAttribute : Attribute
        {
            public string Name { get; }

            public CommandAttribute(string name)
            {
                Name = name.ToLower();
            }
        }

        private void HandleChatCommand(string command)
        {
            InitializeCommands(); // Lazy initialization

            string[] parts = command.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmd = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (commandMap.TryGetValue(cmd, out var action))
            {
                action.Invoke(args);
            }
            else
            {
                Console.WriteLine("Unknown Command.");
            }
        }

        private void InitializeCommands()
        {
            if (commandMap != null)
                return;

            commandMap = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase);

            var methods = GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.GetCustomAttributes(typeof(CommandAttribute), false) is CommandAttribute[] attributes)
                {
                    foreach (var attr in attributes)
                    {
                        if (commandMap.ContainsKey(attr.Name))
                        {
                            Console.WriteLine($"Warning: Duplicate command alias detected: {attr.Name}");
                            continue;
                        }

                        // Handle static vs instance
                        object target = method.IsStatic ? null : this;

                        // Handle parameterless methods
                        if (method.GetParameters().Length == 0)
                        {
                            commandMap[attr.Name] = (args) => method.Invoke(target, null);
                        }
                        // Handle string[] args methods
                        else if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string[]))
                        {
                            commandMap[attr.Name] = (args) => method.Invoke(target, new object[] { args });
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Command '{attr.Name}' has unsupported signature.");
                        }
                    }
                }
            }
        }
        #endregion

        #region Help Command List

        private static readonly (string command, string description)[] commands = new (string, string)[]
        {
            // Showcasing Commands.
            ("cc",                                                                 "Clears the chat. This was made for showcasing."),
            ("brightness [amount]",                                                "Change the brightness. Use '1' for default. This was made for showcasing."),
            ("teleport [x] [y] [z]",                                               "Teleport the player to a new position."),
            ("time [time]",                                                        "Change the worlds time. Use 0-100 for time of day."),
            ("toggleui",                                                           "Toggles the HUD and UI visibility."),

            // General Commands.
            ("help (page)",                                                        "Display all available commands."),
            ("undo (times)",                                                       "Undoes the last action (from history)."),
            ("redo (times)",                                                       "Redoes the last action (from history)."),

            // Navigation Commands.
            ("unstuck",                                                            "Escape from being stuck inside a block."),
            ("ascend (levels)",                                                    "Go up a floor."),
            ("descend (levels)",                                                   "Go down a floor."),
            ("ceil",                                                               "Go to the ceiling."),
            ("thru",                                                               "Pass through walls."),
            ("jumpto",                                                             "Teleport to the cursors location."),
            ("up [amount]",                                                        "Go upwards some distance."),
            ("down [amount]",                                                      "Go downwards some distance."),

            // Selection Commands.
            ("wand [on/off]",                                                      "Get the wand item."),
            ("pos [pos1/pos2..]",                                                  "Set positions."),

            // Region Commands.
            ("set [block(,array)] (hollow)",                                       "Sets all the blocks in the region."),
            ("line [block(,array)] (thickness)",                                   "Draws line segments between two positions."),
            ("replace [source block,(all)] [to block,(all)]",                      "Replace all blocks in the selection with another."),
            ("allexcept [source block(,array)] (to block(,array))",                "Replace all blocks except a desired block pattern."),
            ("massreplace [radii] [source block,(all)] [to block,(all)]",          "Replace all blocks within a circular radii with another."),
            ("walls [block(,array)]",                                              "Build the four sides of the selection."),
            ("smooth (iterations)",                                                "Smooth the elevation in the selection."),
            ("stack (amount) (direction) (useAir)",                                "Repeat the contents of the selection."),
            ("stretch (amount) (direction) (useAir)",                              "Stretch the contents of the selection."),
            ("spell [words(@linebreak)/(/paste)] [block(,array)] (flip) (rotate)", "Draws a text made of blocks relative to position 1."),
            ("hollow (block(,array)) (thickness)",                                 "Hollows out the object contained in this selection."),
            ("fill [block(,array)]",                                               "Fills only the inner-most blocks of an object contained in this selection."),
            ("wrap [replace block(,array)] (exclude surface)",                     "Fills only the outer-most air blocks of an object contained in this selection."),
            ("matrix [radius] [spacing] (snow) (default(,array))",                 "Places your clipboard spaced out in intervals."),
            ("snow [block(,array)] [radius]",                                      "Places a pattern of blocks on ground level around position 1."),
            ("forest [area_size] [density] (max_height)",                          "Make a forest within the region."),
            ("tree (max_height)",                                                  "Make a tree at position 1."),

            // Generation Commands.
            ("floor [block(,array)] [radius] (hollow)",                            "Makes a filled floor."),
            ("cube [block(,array)] [radii] (hollow)",                              "Makes a filled cube."),
            ("prism [block(,array)] [length] [width] (height) (hollow)",           "Makes a filled prism."),
            ("sphere [block(,array)] [radii] (hollow) (height)",                   "Makes a filled sphere."),
            ("pyramid [block(,array)] [size] (hollow)",                            "Makes a filled pyramid."),
            ("cone [block(,array)] [radii] [height] (hollow)",                     "Makes a filled cone."),
            ("cylinder [block(,array)] [radii] [height] (hollow)",                 "Makes a filled cylinder."),
            ("diamond [r block(,array)] [radii] (hollow) (squared)",               "Makes a filled diamond."),
            ("ring [block(,array)] [radius] (hollow)",                             "Makes a filled ring."),
            ("ringarray [block(,array)] [amount] [space]",                         "Makes a hollowed ring at evenly spaced intervals."),
            ("generate [block(,array)] [expression(clipboard)] (hollow)",          "Generates a shape according to a formula."),

            // Schematic and Clipboard Commands.
            ("schematic [save] (saveAir)",                                         "Save your clipboard into a schematic file."),
            ("schematic [load] (loadAir)",                                         "Load a schematic into your clipboard."),
            ("copy",                                                               "Copy the selection to the clipboard."),
            ("cut",                                                                "Cut the selection to the clipboard."),
            ("paste (useAir)",                                                     "Paste the clipboard’s contents."),
            ("rotate (rotateY) (rotateX) (rotateZ)",                               "Rotate the contents of the clipboard."),
            ("flip (direction)",                                                   "Flip the contents of the clipboard across the origin."),
            ("clearclipboard",                                                     "Clear your clipboard."),

            // Tool Commands.
            ("tool [on/off] [/command], " +
                 "tool command [/command]",                                        "Binds a tool to the item in your hand."),

            // Brush Commands.
            ("brush [on/off] (block(,array)) (size), " +
                 "brush block [block(,array)], " +
                 "brush shape [shape], " +
                 "brush size [size], " +
                 "brush height [height], " +
                 "brush hollow [true/false], " +
                 "brush replace [true/false], " +
                 "brush rapid [true/false]",                                       "Brushing commands.")
        };
        #endregion

        #region Chat Command Functions

        // Showcasing Commands.

        #region SHOWCASING COMMANDS ONLY - Remove this category from your project.

        #region /cc

        [Command("/cc")]
        private static void ExecuteCC()
        {
            try
            {
                // Send blank messages to chat to clear chat.
                for (int i = 0; i < 10; i++)
                    Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /brightness

        [Command("/brightness")]
        private static void ExecuteBrightness(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /brightness [amount]");
                return;
            }

            try
            {
                float amount = float.TryParse(args[0], out float a) ? a : 1;
                
                // Check if the brightness is greater then 0. If not, disable.
                if (amount > 1)
                {
                    DNA.CastleMinerZ.UI.InGameHUD.Instance.PlayerHealth = amount;

                    // Display message.
                    Console.WriteLine($"Brightness was set to: '{amount}'.");
                }
                else
                {
                    // Reset the brightness to the default max values.
                    DNA.CastleMinerZ.UI.InGameHUD.Instance.PlayerHealth = 1;

                    // Display message.
                    Console.WriteLine($"Brightness returned to default.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /teleport

        [Command("/teleport")]
        [Command("/tp")]
        private static void ExecuteTeleport(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Command usage /teleport [x] [y] [z]");
                return;
            }

            try
            {
                float xPos = float.TryParse(args[0], out float x) ? x : 0;
                float yPos = float.TryParse(args[1], out float y) ? y : 0;
                float zPos = float.TryParse(args[2], out float z) ? z : 0;

                // Define new position.
                Vector3 newPosition = new Vector3(x, y, z);

                // Teleport the payer to the new position.
                TeleportUser(newPosition, true);

                // Display message.
                Console.WriteLine($"Teleported to: '{new Vector3((int)Math.Round(newPosition.X), (int)Math.Round(newPosition.Y), (int)Math.Round(newPosition.Z))}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /time

        [Command("/time")]
        private static void ExecuteTime(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /time [time]");
                return;
            }

            try
            {
                float time = float.TryParse(args[0], out float t) ? t : 0;

                // Define new time from a 0.0-1.0 range.
                // Ensure to handle value under and over 0-100.
                float newTime = (time < 0) ? 0 : (time > 100) ? 1 : (float)(time / 100.0);

                // Set the time to the new value.
                // Use the none host version of this function.
                DNA.CastleMinerZ.CastleMinerZGame.Instance.GameScreen.Day = newTime;

                // Display message.
                Console.WriteLine($"Time was set to: '{newTime}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /toggleui

        [Command("/toggleui")]
        private static void ExecuteToggleUI()
        {
            try
            {
                // Toggle the _hideUI and avatar visibility bools.
                DNA.CastleMinerZ.UI.InGameHUD._hideUI = !DNA.CastleMinerZ.UI.InGameHUD._hideUI;
                DNA.CastleMinerZ.CastleMinerZGame.Instance.LocalPlayer.Avatar.Visible = !CastleMinerZGame.Instance.LocalPlayer.Avatar.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #endregion

        // General Commands.

        #region /help

        [Command("/help")]
        private static void ExecuteHelp(string[] args)
        {
            int maxLinesPerPage = 7;
            int totalPages = (int)Math.Ceiling((double)commands.Length / maxLinesPerPage);
            int page = 1;

            // If an argument is provided, try to parse it as a page number.
            if (args.Length > 1 || (args.Length == 1 && !int.TryParse(args[0], out page) || page < 1 || page > totalPages))
            {
                Console.WriteLine("ERROR: Command usage /help (optional: page number)");
                return;
            }

            int startIndex = (page - 1) * maxLinesPerPage;
            int endIndex = Math.Min(startIndex + maxLinesPerPage, commands.Length);

            Console.WriteLine($"== Help - Page {page}/{totalPages} ==");
            for (int i = startIndex; i < endIndex; i++)
            {
                Console.WriteLine($"{commands[i].command} - {commands[i].description}");
            }

            if (page < totalPages)
            {
                Console.WriteLine($"== Use \"/help {page + 1}\" for the next page. ==");
            }
        }
        #endregion

        #region /undo

        [Command("/undo")]
        private static void ExecuteUndo(string[] args)
        {
            try
            {
                int times = args.Length > 0 && int.TryParse(args[0], out int t) ? t : 1;

                // Check if any undo actions exist.
                if (UndoStack.Count == 0)
                {
                    Console.WriteLine("No undo actions available.");
                    return;
                }

                // Perform undo actions multiple times based on the 'times' parameter.
                int actionsCount = 0;
                for (int i = 0; i < times; i++)
                {
                    if (UndoStack.Count == 0)
                    {
                        // Console.WriteLine($"Only {i} actions were available.");
                        break;
                    }
                    else
                        actionsCount++;

                    // Run the load undo function from the undo/redo manager.
                    foreach (var action in LoadUndo())
                    {
                        // Get location of block and block ID.
                        Vector3 blockLocation = action.Item1;
                        int block = action.Item2;

                        // Place block if it doesn't already exist. (improves the performance)
                        // If multiple undo's where made, the count is less then 1, make an exception.
                        // This is done encase the start and finish saves where the same nullifying them out.
                        if (GetBlockFromLocation(blockLocation) != block || (times > 1 && UndoStack.Count <= 1))
                            PlaceBlock(blockLocation, block);
                    }
                }

                Console.WriteLine($"Undid '{actionsCount}' action(s) successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /redo

        [Command("/redo")]
        private static void ExecuteRedo(string[] args)
        {
            try
            {
                int times = args.Length > 0 && int.TryParse(args[0], out int t) ? t : 1;

                // Check if any redo actions exist.
                if (RedoStack.Count == 0)
                {
                    Console.WriteLine("No redo actions available.");
                    return;
                }

                // Perform redo actions multiple times based on the 'times' parameter.
                int actionsCount = 0;
                for (int i = 0; i < times; i++)
                {
                    if (RedoStack.Count == 0)
                    {
                        // Console.WriteLine($"Only {i} actions were available.");
                        break;
                    }
                    else
                        actionsCount++;

                    // Run the load redo function from the undo/redo manager.
                    foreach (var action in LoadRedo())
                    {
                        // Get location of block and block ID.
                        Vector3 blockLocation = action.Item1;
                        int block = action.Item2;

                        // Place block if it doesn't already exist. (improves the performance)
                        // If multiple redo's where made, the count is less then 1, make an exception.
                        // This is done encase the start and finish saves where the same nullifying them out.
                        if (GetBlockFromLocation(blockLocation) != block || (times > 1 && RedoStack.Count <= 1))
                            PlaceBlock(blockLocation, block);
                    }
                }

                Console.WriteLine($"Redid '{actionsCount}' action(s) successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Navigation Commands.

        #region /unstuck

        [Command("/unstuck")]
        [Command("/!")]
        private static void ExecuteUnstuck()
        {
            try
            {
                // Get the new location offset.
                Vector3 usersLocation = GetUsersLocation(); // Get the user's current location.
                Vector3 newLocation = usersLocation;

                // Try to ascend first.
                Vector3 nextLocation = GetAscendingVector(newLocation);

                // Stop if no valid location is found.
                if (nextLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(newLocation, false);

                    Console.WriteLine($"Teleported up '1' level!");
                    return;
                }

                // Still stuck, try going through.
                Vector3 cursorLocation = GetUsersCursorLocation();                             // Get the user's cursor location.
                Direction facingDirection = GetFacingDirection(usersLocation, cursorLocation); // Determine the direction the user is facing.
                nextLocation = GetThruVector(newLocation, facingDirection);

                // Stop if no valid location is found.
                if (nextLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(newLocation, false);

                    Console.WriteLine($"Teleported thru '{Math.Round(Vector3.Distance(usersLocation, newLocation))}' blocks!");
                    return;
                }

                // Still stuck, try descending.
                nextLocation = GetDescendingVector(newLocation);

                // Stop if no valid location is found.
                if (nextLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(newLocation, false);

                    Console.WriteLine($"Teleported down '1' level!");
                    return;
                }

                // Still stuck. How did this happen?
                Console.WriteLine("Unable to find a suitable location.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /ascend

        [Command("/ascend")]
        [Command("/asc")]
        private static void ExecuteAscend(string[] args)
        {
            try
            {
                int levels = args.Length > 0 && int.TryParse(args[0], out int l) ? l : 1;

                // Get the new location offset.
                Vector3 usersLocation = GetUsersLocation(); // Get the user's current location.
                Vector3 newLocation = usersLocation;

                // Ascend the specified number of levels.
                int levelCount = 0;
                for (int i = 0; i < levels; i++)
                {
                    Vector3 nextLocation = GetAscendingVector(newLocation);

                    // Stop if no valid location is found.
                    if (nextLocation == newLocation)
                    {
                        Console.WriteLine($"Stopped at level {levelCount}: No further valid location found.");
                        break;
                    }
                    else
                        levelCount++;

                    newLocation = nextLocation;
                }

                // Teleport only if the location changed.
                if (newLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(newLocation, false);

                    Console.WriteLine($"Teleported up {levelCount} level(s)!");
                }
                else
                    Console.WriteLine("No valid location was found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /decend

        [Command("/descend")]
        [Command("/desc")]
        private static void ExecuteDescend(string[] args)
        {
            try
            {
                int levels = args.Length > 0 && int.TryParse(args[0], out int l) ? l : 1;

                // Get the new location offset.
                Vector3 usersLocation = GetUsersLocation(); // Get the user's current location.
                Vector3 newLocation = usersLocation;

                // Descend the specified number of levels.
                int levelCount = 0;
                for (int i = 0; i < levels; i++)
                {
                    Vector3 nextLocation = GetDescendingVector(newLocation);

                    // Stop if no valid location is found.
                    if (nextLocation == newLocation)
                    {
                        Console.WriteLine($"Stopped at level {levelCount}: No further valid location found.");
                        break;
                    }
                    else
                        levelCount++;

                    newLocation = nextLocation;
                }

                // Teleport only if the location changed.
                if (newLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(newLocation, false);

                    Console.WriteLine($"Teleported down {levelCount} level(s)!");
                }
                else
                    Console.WriteLine("No valid location was found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /ceil

        [Command("/ceil")]
        private static void ExecuteCeil()
        {
            try
            {
                // Get the new location offset.
                Vector3 usersLocation = GetUsersLocation(); // Get the user's current location.
                Vector3 newLocation = GetCeilingVector(usersLocation);

                // Check if a valid location was found.
                if (newLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(newLocation, false);

                    Console.WriteLine($"Teleported up '{Math.Round(Vector3.Distance(usersLocation, newLocation))}' blocks!");
                }
                else
                    Console.WriteLine("No valid location was found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /thru

        [Command("/thru")]
        private static void ExecuteThru()
        {
            try
            {
                // Get the new location offset.
                Vector3 usersLocation = GetUsersLocation();                                    // Get the user's current location.
                Vector3 cursorLocation = GetUsersCursorLocation();                             // Get the user's cursor location.
                Direction facingDirection = GetFacingDirection(usersLocation, cursorLocation); // Determine the direction the user is facing.
                Vector3 newLocation = GetThruVector(usersLocation, facingDirection);

                // Check if a valid location was found.
                if (newLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(newLocation, false);

                    Console.WriteLine($"Teleported thru '{Math.Round(Vector3.Distance(usersLocation, newLocation))}' blocks!");
                }
                else
                    Console.WriteLine("No valid location was found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /jumpto
        private static Vector3 lastJumpLocation = new Vector3(0, 0, 0); // Store the last jump location.

        [Command("/jumpto")]
        [Command("/j")]
        private static void ExecuteJumpTo()
        {
            try
            {
                // Get the new location offset.
                Vector3 usersLocation = GetUsersLocation();        // Get the user's current location.
                Vector3 cursorLocation = GetUsersCursorLocation(); // Get the user's cursor location.

                // Check if a valid location was found. Ensure we don't teleport to the same location twice.
                if (lastJumpLocation != cursorLocation && cursorLocation != usersLocation)
                {
                    // Teleport user.
                    TeleportUser(cursorLocation, false);

                    // Store this jump location.
                    lastJumpLocation = cursorLocation;

                    // Feel free to comment this out. Can get annoying.
                    Console.WriteLine($"Teleported '{Math.Round(Vector3.Distance(usersLocation, cursorLocation))}' blocks away!");
                }
                // else
                    // Console.WriteLine("No valid location was found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /up

        [Command("/up")]
        private static async void ExecuteUp(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /up [amount]");
                return;
            }

            try
            {
                int amount = int.TryParse(args[0], out int a) ? a : 15;

                // Get the new location offset. Convert the offsets into integers.
                Vector3 upwardsLocation = new Vector3((int)GetUsersLocation().X, (int)GetUsersLocation().Y, (int)GetUsersLocation().Z);
                upwardsLocation.Y += amount;

                // Ensure the position is within the bounds of the world.
                if (upwardsLocation.Y <= WorldHeights.Item2)
                {
                    PlaceBlock(upwardsLocation, 48);      // GlassMystery.
                    await Task.Delay(100);                // Add short wait.
                    upwardsLocation.Y += 1;               // Place user on top. (adjust for your user offset)
                    TeleportUser(upwardsLocation, false); // Teleport user.

                    Console.WriteLine($"Teleported up '{amount}' blocks!");
                }
                else
                    Console.WriteLine($"Location 'Y:{Math.Round(upwardsLocation.Y)}' is out of bounds. Max: '{WorldHeights.Item2}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /down

        [Command("/down")]
        private static async void ExecuteDown(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /down [amount]");
                return;
            }

            try
            {
                int amount = int.TryParse(args[0], out int a) ? a : 15;

                // Get the new location offset. Convert the offsets into integers.
                Vector3 upwardsLocation = new Vector3((int)GetUsersLocation().X, (int)GetUsersLocation().Y, (int)GetUsersLocation().Z);
                upwardsLocation.Y -= amount;

                // Ensure the position is within the bounds of the world.
                if (upwardsLocation.Y >= WorldHeights.Item1)
                {
                    PlaceBlock(upwardsLocation, 48);      // GlassMystery.
                    await Task.Delay(100);                // Add short wait.
                    upwardsLocation.Y += 1;               // Place user on top. (adjust for your user offset)
                    TeleportUser(upwardsLocation, false); // Teleport user.

                    Console.WriteLine($"Teleported down '{amount}' blocks!");
                }
                else
                    Console.WriteLine($"Location 'Y:{Math.Round(upwardsLocation.Y)}' is out of bounds. Max: '{WorldHeights.Item1}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Selection Commands.

        #region /wand

        [Command("/wand")]
        private void ExecuteWand(string[] args) // Don't give 'static' for tool command.
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /wand [on/off]");
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        if (args.Length > 0 && args.Length < 2) // Valid: 0+; 1-2.
                        {
                            // Give the user a compass.
                            /*
                            _game.LocalPlayer.PlayerInventory.AddInventoryItem(
                                DNA.CastleMinerZ.Inventory.InventoryItem.CreateItem(WandItemID, 1),
                                false
                            );
                            */

                            Timer wandTimer = new Timer() { Interval = 1 };
                            wandTimer.Tick += WorldWand_Tick;
                            wandTimer.Start();

                            _wandEnabled = true;
                            Console.WriteLine("Wand Activated!");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing parameter. Usage: /wand [on/off]");
                            return;
                        }
                        break;

                    case "off":
                        if (args.Length > 0 && args.Length < 2) // Valid: 0+; 1-2.
                        {
                            _wandEnabled = false;
                            Console.WriteLine("Wand Deactivated!");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing parameter. Usage: /wand [on/off]");
                            return;
                        }
                        break;

                    default:
                        Console.WriteLine("ERROR: Command usage /wand [on/off]");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /pos

        [Command("/pos")]
        private static void ExecutePos(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /pos [pos1/pos2..]");
                return;
            }

            try
            {
                int point = int.TryParse(args[0], out int p) ? p : 1;

                // Check what position to set.
                if (point == 1)
                    _pointToLocation1 = GetUsersLocation();
                else if (point == 2)
                    _pointToLocation2 = GetUsersLocation();

                // Ensure point is within range.
                if (point == 1 || point == 2)
                    Console.WriteLine($"Position {point} ({(point == 1 ? $"{Math.Round(_pointToLocation1.X)}, {Math.Round(_pointToLocation1.Y)}, {Math.Round(_pointToLocation1.Z)}" : $"{Math.Round(_pointToLocation2.X)}, {Math.Round(_pointToLocation2.Y)}, {Math.Round(_pointToLocation2.Z)}")}) has been set!");
                else
                    Console.WriteLine($"Position {point} is not valid!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Region Commands.

        #region /set

        [Command("/set")]
        private static void ExecuteSet(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /set [block(,array)] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                bool hollow = args.Length > 1 && args[1].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Check and make sure the region contains less than a million blocks.
                // Make 'No' the highlighted option. Helps mitigate issues when using '/tool'.
                if (CalculateBlockCount(definedRegion.Position1, definedRegion.Position2) > 1000000 &&
                    MessageBox.Show("This region contains over a million blocks.\n\nDo you want to continue anyways?", 
                                    "WE: Woah! That's a ton of blocks!", 
                                    MessageBoxButtons.YesNo, 
                                    MessageBoxIcon.Warning, 
                                    MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    Console.WriteLine("Operation canceled.");
                    return;
                }

                // FillRegion(Region region, bool hollow, int ignoreBlock = -1).
                // Check if the to-block pattern is only air, and if so, have the region skip saving it.
                var region = (blockPatternNumbers.Length == 1 && blockPatternNumbers[0] == AirID) ? FillRegion(definedRegion, hollow, AirID) : FillRegion(definedRegion, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /line

        [Command("/line")]
        private static void ExecuteLine(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /line [block(,array)] (thickness)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int thickness = args.Length > 1 && int.TryParse(args[1], out int t) ? t : 0;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // MakeLine(Region region, int thickness).
                var region = MakeLine(definedRegion, thickness);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /replace

        [Command("/replace")]
        [Command("/rep")]
        [Command("/re")]
        private static void ExecuteReplace(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /replace [source block,(all)] [to block,(all)]");
                return;
            }

            try
            {
                string searchPattern = int.TryParse(args[0], out int r) ? r.ToString() : !string.IsNullOrEmpty(args[0]) ? args[0] : "-1"; // Use an invalid id so it fails.
                string replacePattern = !string.IsNullOrEmpty(args[1]) ? args[1] : "1";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                searchPattern = (searchPattern == "all") ? "all" : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(searchPattern);
                replacePattern = (replacePattern == "all") ? "all" : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern);

                // Make sure the input is within the min/max.
                int[] searchPatternNumbers = (searchPattern == "all") ? new int[1] : (!string.IsNullOrEmpty(searchPattern)) ? searchPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (searchPatternNumbers.Length == 0 || searchPatternNumbers.Min() < BlockIDValues.Item1 || searchPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }
                int[] replacePatternNumbers = (replacePattern == "all") ? new int[1] : (!string.IsNullOrEmpty(replacePattern)) ? replacePattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (replacePatternNumbers.Length == 0 || replacePatternNumbers.Min() < BlockIDValues.Item1 || replacePatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Use fill region to define a rectangular area to search in.
                // FillRegion(Region region, bool hollow, int ignoreBlock = -1).
                var region = FillRegion(definedRegion, false);

                // Save the existing region and clear the upcoming redo.
                if (searchPattern == "all")
                    SaveUndo(region);
                else
                    SaveUndo(region, int.Parse(searchPattern));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get the current block type.
                    int currentBlock = GetBlockFromLocation(i);

                    // Check if the current block is a block to replace.
                    if ((searchPattern == "all" && currentBlock != AirID) || currentBlock.ToString() == searchPattern) // Make sure not to replace 'air' when using 'all' mode.
                    {
                        // Get random block from input.
                        HashSet<int> excludedBlocks = new HashSet<int> { AirID, 26 }; // IDs to exclude. Block ID 26 'Torch' crashes.
                        int replaceBlock = (replacePattern == "all") ? GetRandomBlock(excludedBlocks) : GetRandomBlockFromPattern(replacePattern);

                        // Place block if it doesn't already exist. (improves the performance) 
                        if (GetBlockFromLocation(i) != replaceBlock)
                        {
                            PlaceBlock(i, replaceBlock);

                            // Add block to redo.
                            redoBuilder.Add(new Tuple<Vector3, int>(i, replaceBlock));
                        }
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /allexcept

        [Command("/allexcept")]
        [Command("/allex")]
        private static void ExecuteAllExcept(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /allexcept [source block(,array)] (to block(,array))");
                return;
            }

            try
            {
                string exceptPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                string replacePattern = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : "0";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                exceptPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(exceptPattern);
                replacePattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern);

                // Make sure the input is within the min/max.
                int[] exceptPatternNumbers = (!string.IsNullOrEmpty(exceptPattern)) ? exceptPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (exceptPatternNumbers.Length == 0 || exceptPatternNumbers.Min() < BlockIDValues.Item1 || exceptPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }
                int[] replacePatternNumbers = (!string.IsNullOrEmpty(replacePattern)) ? replacePattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (replacePatternNumbers.Length == 0 || replacePatternNumbers.Min() < BlockIDValues.Item1 || replacePatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Use fill region to define a rectangular area to search in.
                // FillRegion(Region region, bool hollow, int ignoreBlock = -1).
                var region = FillRegion(definedRegion, false);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get the current block type.
                    int currentBlock = GetBlockFromLocation(i);

                    // Convert string to a list of integers.
                    var excludedBlocks = exceptPattern.Split(',').Select(int.Parse).ToList();

                    // Check if the current block is not excluded, and its not air, place new block.
                    if ((!excludedBlocks.Contains(currentBlock)) && currentBlock != AirID)
                    {
                        // Get random block from input.
                        int replaceBlock = GetRandomBlockFromPattern(replacePattern);

                        // Place block if it doesn't already exist. (improves the performance) 
                        if (GetBlockFromLocation(i) != replaceBlock)
                        {
                            PlaceBlock(i, replaceBlock);

                            // Add block to redo.
                            redoBuilder.Add(new Tuple<Vector3, int>(i, replaceBlock));
                        }
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /massreplace

        [Command("/massreplace")]
        [Command("/massre")]
        private static void ExecuteMassReplace(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Command usage /massreplace [radii] [source block,(all)] [to block,(all)]");
                return;
            }

            try
            {
                int radii = int.TryParse(args[0], out int r) ? r : 1;
                string searchPattern = !string.IsNullOrEmpty(args[1]) ? args[1] : "2";
                string replacePattern = !string.IsNullOrEmpty(args[2]) ? args[2] : "1";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                searchPattern = (searchPattern == "all") ? "all" : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(searchPattern);
                replacePattern = (replacePattern == "all") ? "all" : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern);

                // Make sure the input is within the min/max.
                int[] searchPatternNumbers = (searchPattern == "all") ? new int[1] : (!string.IsNullOrEmpty(searchPattern)) ? searchPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (searchPatternNumbers.Length == 0 || searchPatternNumbers.Min() < BlockIDValues.Item1 || searchPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }
                int[] replacePatternNumbers = (replacePattern == "all") ? new int[1] : (!string.IsNullOrEmpty(replacePattern)) ? replacePattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (replacePatternNumbers.Length == 0 || replacePatternNumbers.Min() < BlockIDValues.Item1 || replacePatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Get the further distance from the world boundaries.
                int furthestDistance = (int)Math.Max(Math.Abs(_pointToLocation1.Y - WorldHeights.Item2), Math.Abs(_pointToLocation1.Y - WorldHeights.Item1));

                // Get the center point.
                Vector3 centerOffset = new Vector3(_pointToLocation1.X, _pointToLocation1.Y - (furthestDistance / 2), _pointToLocation1.Z);

                // MakeCylinder(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, int ignoreBlock = -1).
                // Check if the from-block pattern contains air, and if so, have the region save it.
                var region = (searchPattern == "all" || searchPatternNumbers.Contains(AirID)) ? MakeCylinder(centerOffset, radii, radii, furthestDistance, false) : MakeCylinder(centerOffset, radii, radii, WorldHeights.Item2 * 2, false, AirID);

                // Save the existing region and clear the upcoming redo.
                if (searchPattern == "all")
                    SaveUndo(region);
                else
                    SaveUndo(region, int.Parse(searchPattern));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get the current block type.
                    int currentBlock = GetBlockFromLocation(i);

                    // Check if the current block is a block to replace.
                    if ((searchPattern == "all" && currentBlock != AirID) || currentBlock.ToString() == searchPattern) // Make sure not to replace 'air' when using 'all' mode.
                    {
                        // Get random block from input.
                        HashSet<int> excludedBlocks = new HashSet<int> { AirID, 26 }; // IDs to exclude. Block ID 26 'Torch' crashes.
                        int replaceBlock = (replacePattern == "all") ? GetRandomBlock(excludedBlocks) : GetRandomBlockFromPattern(replacePattern);

                        // Place block if it doesn't already exist. (improves the performance) 
                        if (GetBlockFromLocation(i) != replaceBlock)
                        {
                            PlaceBlock(i, replaceBlock);

                            // Add block to redo.
                            redoBuilder.Add(new Tuple<Vector3, int>(i, replaceBlock));
                        }
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /walls

        [Command("/walls")]
        private static void ExecuteWalls(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /walls [block(,array)]");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // MakeWalls(Region region).
                var region = MakeWalls(definedRegion);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /smooth

        [Command("/smooth")]
        private static void ExecuteSmooth(string[] args)
        {
            try
            {
                int iterations = args.Length > 0 && int.TryParse(args[0], out int i) ? i : 1;

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // SmoothTerrain(Region region, int iterations).
                var smoothedTerrain = SmoothTerrain(definedRegion, iterations);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(smoothedTerrain));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var t in smoothedTerrain)
                {
                    // Get location of block and block ID.
                    Vector3 blockLocation = t.Item1;
                    int block = t.Item2;

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != block)
                    {
                        PlaceBlock(blockLocation, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /stack

        [Command("/stack")]
        private static void ExecuteStack(string[] args)
        {
            try
            {
                // Default settings.
                bool useAir = true; // Enabled by default.
                int stackCount = args.Length > 0 && int.TryParse(args[0], out int s) ? s : 1;
                Direction stackDirection = Direction.posX;

                // If only the amount is provided, use the cursor location to determine the direction.
                if (args.Length == 1)
                {
                    Vector3 cursorLocation = GetUsersCursorLocation();
                    stackDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                }
                else if (args.Length == 2)
                {
                    // Try to determine if the second argument is a boolean.
                    if (bool.TryParse(args[1], out bool parsedBool))
                    {
                        // When the boolean parses successfully, it means the user omitted a direction.
                        useAir = parsedBool;
                        Vector3 cursorLocation = GetUsersCursorLocation();
                        stackDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                    }
                    else
                    {
                        // Otherwise, assume the second argument is a direction.
                        if (!Enum.TryParse<Direction>(args[1], true, out stackDirection))
                        {
                            // If parsing fails, fall back to the cursor location.
                            Vector3 cursorLocation = GetUsersCursorLocation();
                            stackDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                        }
                    }
                }
                else if (args.Length >= 3)
                {
                    // Assume the user provided both a direction and a useAir flag.
                    if (!Enum.TryParse<Direction>(args[1], true, out stackDirection))
                    {
                        // If the direction string doesn’t match, fallback to the cursor direction.
                        Vector3 cursorLocation = GetUsersCursorLocation();
                        stackDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                    }
                    // Parse the boolean from the third argument.
                    if (bool.TryParse(args[2], out bool parsedAir))
                    {
                        useAir = parsedAir;
                    }
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // StackRegion(Region region, Direction facingDirection, int stackCount, bool useAir = true).
                var stackedBlocks = StackRegion(definedRegion, stackDirection, stackCount, useAir);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(stackedBlocks));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var i in stackedBlocks)
                {
                    // Get location of block.
                    Vector3 blockLocation = i.Item1;

                    // Get block from location.
                    int blockAtLocation = GetBlockFromLocation(blockLocation);

                    // Get block from input.
                    int block = i.Item2;

                    // Place block if it doesn't already exist. (improves the performance)
                    if (blockAtLocation != block)
                    {
                        PlaceBlock(blockLocation, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, block));
                    }
                }

                // Save the builder to new redo.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{stackedBlocks.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /stretch

        [Command("/stretch")]
        [Command("/str")]
        private static void ExecuteStretch(string[] args)
        {
            try
            {
                // Default settings.
                bool useAir = true; // Enabled by default.
                double stretchFactor = args.Length > 0 && double.TryParse(args[0], out double f) ? f : 1.0;
                Direction stretchDirection = Direction.posX;

                // If only the amount is provided, use the cursor location to determine the direction.
                if (args.Length == 1)
                {
                    Vector3 cursorLocation = GetUsersCursorLocation();
                    stretchDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                }
                else if (args.Length == 2)
                {
                    // Try to determine if the second argument is a boolean.
                    if (bool.TryParse(args[1], out bool parsedBool))
                    {
                        // When the boolean parses successfully, it means the user omitted a direction.
                        useAir = parsedBool;
                        Vector3 cursorLocation = GetUsersCursorLocation();
                        stretchDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                    }
                    else
                    {
                        // Otherwise, assume the second argument is a direction.
                        if (!Enum.TryParse<Direction>(args[1], true, out stretchDirection))
                        {
                            // If parsing fails, fall back to the cursor location.
                            Vector3 cursorLocation = GetUsersCursorLocation();
                            stretchDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                        }
                    }
                }
                else if (args.Length >= 3)
                {
                    // Assume the user provided both a direction and a useAir flag.
                    if (!Enum.TryParse<Direction>(args[1], true, out stretchDirection))
                    {
                        // If the direction string doesn’t match, fallback to the cursor direction.
                        Vector3 cursorLocation = GetUsersCursorLocation();
                        stretchDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                    }
                    // Parse the boolean from the third argument.
                    if (bool.TryParse(args[2], out bool parsedAir))
                    {
                        useAir = parsedAir;
                    }
                }

                // Define the selection region.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Ensure the stretchDirection is valid.
                HashSet<Tuple<Vector3, int>> stretchedBlocks;
                if (stretchDirection == Direction.posX || stretchDirection == Direction.negX
                    || stretchDirection == Direction.posZ || stretchDirection == Direction.negZ
                    || stretchDirection == Direction.Up || stretchDirection == Direction.Down)
                {
                    // StretchRegion(Region region, Direction stretchDirection, double stretchFactor, bool useAir = true).
                    stretchedBlocks = StretchRegion(definedRegion, stretchDirection, stretchFactor, useAir);
                }
                else
                {
                    // An invalid direction was thrown. This should never happen unless its 4D. (ex: posW, negW).
                    Console.WriteLine($"ERROR: Invalid direction.");
                    return;
                }

                // Save current state for undo and clear any existing redo history.
                SaveUndo(ExtractVector3HashSet(stretchedBlocks));
                ClearRedo();

                // Apply the changed blocks.
                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var tuple in stretchedBlocks)
                {
                    Vector3 newLocation = tuple.Item1;
                    int blockType = tuple.Item2;

                    if (GetBlockFromLocation(newLocation) != blockType)
                    {
                        PlaceBlock(newLocation, blockType);
                        redoBuilder.Add(new Tuple<Vector3, int>(newLocation, blockType));
                    }
                }

                SaveUndo(redoBuilder);
                Console.WriteLine($"{stretchedBlocks.Count} blocks have been modified!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /spell

        [Command("/spell")]
        private static void ExecuteSpell(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /spell [words(@linebreak)/(/paste)] [block(,array)] (flip) (rotate)");
                return;
            }

            try
            {
                string words = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                string blockPattern = !string.IsNullOrEmpty(args[1]) ? args[1] : "1";
                bool flipAxis = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);
                bool rotate90 = args.Length > 3 && args[3].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeWords(Vector3 pos, string wordString, bool flipAxes = false, bool rotate90 = false).
                var region = MakeWords(_pointToLocation1, words, flipAxis, rotate90);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /hollow

        [Command("/hollow")]
        private static void ExecuteHollow(string[] args)
        {
            try
            {
                string replacePattern = args.Length > 0 && !string.IsNullOrEmpty(args[0]) ? args[0] : "0";
                int thickness = args.Length > 1 && int.TryParse(args[1], out int t) ? t : 1;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                replacePattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern);

                // Make sure the input is within the min/max.
                int[] replacePatternNumbers = (!string.IsNullOrEmpty(replacePattern)) ? replacePattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (replacePatternNumbers.Length == 0 || replacePatternNumbers.Min() < BlockIDValues.Item1 || replacePatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // HollowObject(Region region, int thickness).
                var replaceBlockPattern = replacePattern.Split(',').Select(int.Parse).ToList();
                var region = HollowObject(definedRegion, thickness);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(replacePattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /fill

        [Command("/fill")]
        private static void ExecuteFill(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /fill [block(,array)]");
                return;
            }

            try
            {
                string replacePattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                replacePattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern);

                // Make sure the input is within the min/max.
                int[] replacePatternNumbers = (!string.IsNullOrEmpty(replacePattern)) ? replacePattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (replacePatternNumbers.Length == 0 || replacePatternNumbers.Min() < BlockIDValues.Item1 || replacePatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // FillHollowObject(Region region).
                var replaceBlockPattern = replacePattern.Split(',').Select(int.Parse).ToList();
                var region = FillHollowObject(definedRegion);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(replacePattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /wrap

        [Command("/wrap")]
        private static void ExecuteWrap(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /wrap [replace block(,array)] (exclude surface)");
                return;
            }

            try
            {
                string replacePattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "0";
                bool excludeSurface = args.Length > 1 && args[1].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                replacePattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern);

                // Make sure the input is within the min/max.
                int[] replacePatternNumbers = (!string.IsNullOrEmpty(replacePattern)) ? replacePattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (replacePatternNumbers.Length == 0 || replacePatternNumbers.Min() < BlockIDValues.Item1 || replacePatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // WrapObject(Region region, List<int> replaceBlockPattern, bool excludeSurface).
                var replaceBlockPattern = replacePattern.Split(',').Select(int.Parse).ToList();
                var region = WrapObject(definedRegion, replaceBlockPattern, excludeSurface);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(replacePattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /matrix

        [Command("/matrix")]
        private static void ExecuteMatrix(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /matrix [radius] [spacing] (snow) (default(,array))");
                return;
            }

            // Ensure the copied clipboard is full.
            if (copiedRegion.Count() == 0)
            {
                Console.WriteLine("ERROR: You need to first copy a region.");
                return;
            }

            try
            {
                int radius = int.TryParse(args[0], out int r) ? r : 50;
                int spacing = int.TryParse(args[1], out int s) ? s : 8;
                bool snow = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);
                string optionalBlockPattern = args.Length > 3 && !string.IsNullOrEmpty(args[3]) ? args[3] : "";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                optionalBlockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(optionalBlockPattern);

                // Make sure the input is within the min/max.
                int[] optionalBlockPatternNumbers = (!string.IsNullOrEmpty(optionalBlockPattern)) ? optionalBlockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (optionalBlockPatternNumbers.Length == 0 || optionalBlockPatternNumbers.Min() < BlockIDValues.Item1 || optionalBlockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeMatrix(Vector3 pos, int radius, int spacing, bool enableSnow, string optionalBlockPattern).
                var region = MakeMatrix(_pointToLocation1, radius, spacing, snow, optionalBlockPattern);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var i in region)
                {
                    // Get location of block and block ID.
                    Vector3 blockLocation = i.Item1;
                    int block = i.Item2;

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != block)
                    {
                        PlaceBlock(blockLocation, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{redoBuilder.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /snow

        [Command("/snow")]
        private static void ExecuteSnow(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /snow [block(,array)] [radius]");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radius = int.TryParse(args[1], out int r) ? r : 10;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeSnow(Vector3 center, int radius).
                var region = MakeSnow(_pointToLocation1, radius);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /forest

        [Command("/forest")]
        private static void ExecuteForest(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /forest [density] (max height)");
                return;
            }

            try
            {
                // int treeAreaSquared = int.TryParse(args[0], out int a) ? a : 10;
                int treeDensity = int.TryParse(args[0], out int d) ? d : 20;
                int treeMaxHeight = args.Length > 1 && int.TryParse(args[1], out int h) ? h : 8;

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // MakeForest(Region pos, int density, int max_height).
                var region = MakeForest(definedRegion, treeDensity, treeMaxHeight);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var i in region)
                {
                    // Get location of block and block ID.
                    Vector3 blockLocation = i.Item1;
                    int block = i.Item2;

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != block)
                    {
                        PlaceBlock(blockLocation, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"Forest built with a density of '{treeDensity}' and with max tree heights of '{treeMaxHeight}'!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /tree

        [Command("/tree")]
        private static void ExecuteTree(string[] args)
        {
            try
            {
                int treeMaxHeight = args.Length > 0 && int.TryParse(args[0], out int h) ? h : 8;

                // MakeTree(int worldX, int worldZ, int maxHeight).
                var region = MakeTree((int)_pointToLocation1.X, (int)_pointToLocation1.Z, treeMaxHeight);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var i in region)
                {
                    // Get location of block and block ID.
                    Vector3 blockLocation = i.Item1;
                    int block = i.Item2;

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != block)
                    {
                        PlaceBlock(blockLocation, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"Tree built with a max possible height of '{treeMaxHeight}'!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Generation Commands.

        #region /floor

        [Command("/floor")]
        private static void ExecuteFloor(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /floor [block(,array)] [radius] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radius = int.TryParse(args[1], out int r) ? r : 5;
                bool hollow = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeFloor(Vector3 pos, int size, bool hollow, int ignoreBlock = -1).
                var region = MakeFloor(_pointToLocation1, radius, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /cube

        [Command("/cube")]
        private static void ExecuteCube(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /cube [block(,array)] [radii] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radii = int.TryParse(args[1], out int r) ? r : 5;
                bool hollow = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeCube(Vector3 pos, int radii, bool hollow, int ignoreBlock = -1).
                var region = MakeCube(_pointToLocation1, radii, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /prism

        [Command("/prism")]
        private static void ExecutePrism(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Command usage /prism [block(,array)] [length] [width] (height) (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int length = int.TryParse(args[1], out int l) ? l : 10;
                int width = int.TryParse(args[2], out int w) ? w : 5;
                int height = args.Length > 3 && int.TryParse(args[3], out int h) ? h : -1; // If not specified, make the triangle equilateral.
                bool hollow = args.Length > 4 && args[4].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeTriangularPrism(Vector3 pos, int length, int width, int height, bool hollow, int ignoreBlock = -1).
                var region = MakeTriangularPrism(_pointToLocation1, length, width, height, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /sphere

        [Command("/sphere")]
        private static void ExecuteSphere(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /sphere [block(,array)] [radii] (hollow) (height)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radii = int.TryParse(args[1], out int r) ? r : 5;
                bool hollow = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);
                int height = args.Length > 3 && int.TryParse(args[3], out int h) ? h : radii;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeSphere(Vector3 pos, double radiusX, double radiusY, double radiusZ, bool hollow, int ignoreBlock = -1).
                var region = MakeSphere(_pointToLocation1, radii, height, radii, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /pyramid

        [Command("/pyramid")]
        private static void ExecutePyramid(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /pyramid [block(,array)] [size] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int size = int.TryParse(args[1], out int s) ? s : 5;
                bool hollow = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakePyramid(Vector3 pos, int size, bool hollow, int ignoreBlock = -1).
                var region = MakePyramid(_pointToLocation1, size, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /cone

        [Command("/cone")]
        private static void ExecuteCone(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Command usage /cone [block(,array)] [radii] [height] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radii = int.TryParse(args[1], out int r) ? r : 5;
                int height = int.TryParse(args[2], out int h) ? h : 10;
                bool hollow = args.Length > 3 && args[3].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeCone(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, double thickness, int ignoreBlock = -1).
                var region = MakeCone(_pointToLocation1, radii, radii, height, hollow, 1);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /cylinder

        [Command("/cylinder")]
        [Command("/cyl")]
        private static void ExecuteCylinder(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Command usage /cylinder [block(,array)] [radii] [height] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radii = int.TryParse(args[1], out int r) ? r : 5;
                int height = int.TryParse(args[2], out int h) ? h : 10;
                bool hollow = args.Length > 3 && args[3].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeCylinder(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, int ignoreBlock = -1).
                var region = MakeCylinder(_pointToLocation1, radii, radii, height, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /diamond

        [Command("/diamond")]
        private static void ExecuteDiamond(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /diamond [r block(,array)] [radii] (hollow) (squared)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radii = int.TryParse(args[1], out int r) ? r : 5;
                bool hollow = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);
                bool squared = args.Length > 3 && args[3].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeDiamond(Vector3 pos, int size, bool hollow, int ignoreBlock = -1).
                var region = MakeDiamond(_pointToLocation1, radii, hollow, squared);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /ring

        [Command("/ring")]
        private static void ExecuteRing(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /ring [block(,array)] [radius] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radius = int.TryParse(args[1], out int r) ? r : 5;
                bool hollow = args.Length > 2 && args[2].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeRing(Vector3 pos, double radius, bool hollow, int ignoreBlock = -1).
                var region = MakeRing(_pointToLocation1, radius, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /ringarray

        [Command("/ringarray")]
        [Command("/ringa")]
        private static void ExecuteRingArray(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Command usage /ringarray [block(,array)] [amount] [space]");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int amount = int.TryParse(args[1], out int a) ? a : 5;
                int space = int.TryParse(args[2], out int s) ? s : 1;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // MakeRing(Vector3 pos, double radius, bool hollow, int ignoreBlock = -1).
                HashSet<Vector3> region = new HashSet<Vector3>();
                for (int i = 0; i < amount; i++)
                    region.UnionWith(MakeRing(_pointToLocation1, i * space, true));

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 i in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(i) != block)
                    {
                        PlaceBlock(i, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(i, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /generate

        [Command("/generate")]
        [Command("/gen")]
        [Command("/g")]
        private static void ExecuteGenerate(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /generate [block(,array)] [expression(clipboard)] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";

                // Determine if the last argument is the hollow flag.
                bool hollowSpecified = args.Length > 1 &&
                    (args[args.Length - 1].Equals("true", StringComparison.OrdinalIgnoreCase) ||
                     args[args.Length - 1].Equals("false", StringComparison.OrdinalIgnoreCase));

                bool hollow = false;
                string expression;

                if (hollowSpecified)
                {
                    // Last token is the hollow flag.
                    hollow = bool.Parse(args[args.Length - 1]);
                    // Join all tokens from index 1 to the second-to-last token.
                    expression = string.Join(" ", args, 1, args.Length - 2);
                }
                else
                {
                    // Join all tokens from index 1 onward as the expression.
                    expression = string.Join(" ", args, 1, args.Length - 1);
                }

                // If the user has specified "clipboard" as the expression, fetch the clipboard text.
                if (string.Equals(expression.Trim(), "clipboard", StringComparison.OrdinalIgnoreCase))
                {
                    expression = Clipboard.GetText();
                }

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                // Make sure the input is within the min/max.
                int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                {
                    Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                    return;
                }

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // MakeShape(Vector3 pos, string expression, bool hollow, int ignoreBlock = -1).
                var region = MakeShape(definedRegion, expression, hollow);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var i in region)
                {
                    // Get location of block and block ID.
                    Vector3 blockLocation = i.Item1;
                    int block = i.Item2;

                    // Check if output is -1, use random block from input.
                    if (block == -1)
                        block = GetRandomBlockFromPattern(blockPattern);
                    else
                        if (block < BlockIDValues.Item1 || block > BlockIDValues.Item2) // Ensure the returning value is valid.
                            block = GetRandomBlockFromPattern(blockPattern);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != block)
                    {
                        PlaceBlock(blockLocation, block);
                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);
                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Schematic and Clipboard Commands.

        #region /schem

        [Command("/schematic")]
        [Command("/schem")]
        private static void ExecuteSchematic(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /schematic [save/load] (useAir)");
                return;
            }

            try
            {
                bool useAir = true; // Enabled by default.
                string commandSuffex = !string.IsNullOrEmpty(args[0]) ? args[0] : "";
                if (args.Length > 1 && args[1].Equals("false", StringComparison.OrdinalIgnoreCase)) useAir = false;

                // Check for suffix options.
                if (commandSuffex == "save")
                {
                    // Ensure the copied clipboard is full.
                    if (copiedRegion.Count() == 0)
                    {
                        Console.WriteLine("ERROR: You need to first copy a region.");
                        return;
                    }

                    // Launch an open file dialog to get the name.
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Title = "Select Save Location",
                        Filter = "Schem Files|*.schem",
                        InitialDirectory = Environment.CurrentDirectory
                    };

                    if (saveFileDialog.ShowDialog() == DialogResult.OK) // For WinForms
                    {
                        // Define main file info.
                        FileInfo schemLocation = new FileInfo(saveFileDialog.FileName);

                        // Save the regions data to a file.
                        SaveSchematic(copiedRegion, schemLocation, saveAir: useAir);
                        // Console.WriteLine($"Schematic '{schemLocation.Name}' has been saved successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Save operation canceled.");
                    }
                }
                else if (commandSuffex == "load")
                {
                    // Launch an open file dialog to get the name.
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Title = "Select Schematic File",
                        Filter = "Schem Files|*.schem",
                        InitialDirectory = Environment.CurrentDirectory
                    };

                    if (openFileDialog.ShowDialog() == DialogResult.OK) // For WinForms
                    {
                        // Define main file info.
                        FileInfo schemLocation = new FileInfo(openFileDialog.FileName);

                        // Save the schematics region data to the clipboard.
                        LoadSchematic(schemLocation, useAir);
                        // Console.WriteLine($"Schematic '{schemLocation.Name}' has loaded to the clipboard successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Load operation canceled.");
                    }
                }
                else
                {
                    // No valid argument.
                    Console.WriteLine($"ERROR: Argument was not valid.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Clipboard.SetText(ex.Message);
            }
        }
        #endregion

        #region /copy

        [Command("/copy")]
        private static void ExecuteCopy()
        {
            try
            {
                // Define location data.
                Region region = new Region(_pointToLocation1, _pointToLocation2);

                // Save copy data.
                CopyRegion(region);

                Console.WriteLine($"Region was copied.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /cut

        [Command("/cut")]
        private static void ExecuteCut()
        {
            try
            {
                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Check and make sure the region contains less than a million blocks.
                // Make 'No' the highlighted option. Helps mitigate issues when using '/tool'.
                if (CalculateBlockCount(definedRegion.Position1, definedRegion.Position2) > 1000000 &&
                    MessageBox.Show("This region contains over a million blocks.\n\nDo you want to continue anyways?", 
                                    "WE: Woah! That's a ton of blocks!", 
                                    MessageBoxButtons.YesNo, 
                                    MessageBoxIcon.Warning, 
                                    MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    Console.WriteLine("Operation canceled.");
                    return;
                }

                // Save copy data.
                CopyRegion(definedRegion);

                // FillRegion(Region region, bool hollow, int ignoreBlock = -1).
                var region = FillRegion(definedRegion, false, AirID);

                // Delete the contents of this region.
                foreach (Vector3 i in region)
                {
                    // Remove blocks that are not already air. (improves the performance)
                    if (GetBlockFromLocation(i) != AirID)
                    {
                        PlaceBlock(i, AirID);
                    }
                }

                Console.WriteLine($"Region was cut and copied to your clipboard.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /paste

        [Command("/paste")]
        private static void ExecutePaste(string[] args)
        {
            // Ensure the copied clipboard is full.
            if (copiedRegion.Count() == 0)
            {
                Console.WriteLine("ERROR: You need to first copy a region.");
                return;
            }

            try
            {
                bool useAir = true; // Enabled by default.
                if (args.Length > 0 && args[0].Equals("false", StringComparison.OrdinalIgnoreCase)) useAir = false;

                // PasteRegion(Region region).
                var region = PasteRegion(_pointToLocation1);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var i in region)
                {
                    // Get location of block and block ID.
                    Vector3 blockLocation = i.Item1;
                    int block = i.Item2;

                    // Check if useAir is disabled and if so, skip placing air blocks.
                    if (!useAir && block == AirID) continue;

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != block)
                    {
                        PlaceBlock(blockLocation, block);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, block));
                    }
                }

                // Save the actions to undo stack.
                SaveUndo(redoBuilder);

                Console.WriteLine($"{region.Count} blocks have been replaced!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /rotate

        [Command("/rotate")]
        private static void ExecuteRotate(string[] args)
        {
            // Ensure the copied clipboard is full.
            if (copiedRegion.Count() == 0)
            {
                Console.WriteLine("ERROR: You need to first copy a region.");
                return;
            }

            try
            {
                int rotateY = args.Length > 0 && int.TryParse(args[0], out int rY) ? rY : 90;
                int rotateX = args.Length > 1 && int.TryParse(args[1], out int rX) ? rX : 0;
                int rotateZ = args.Length > 2 && int.TryParse(args[2], out int rZ) ? rZ : 0;

                // Ensure all rotations are valid.
                if (!(IsValidRotation(rotateY) && IsValidRotation(rotateX) && IsValidRotation(rotateZ)))
                {
                    Console.WriteLine($"ERROR: One or more rotations are invalid. Use: (90, 180, 240, 360)");
                    return;
                }

                // Apply the clipboard rotations.
                RotateClipboard(rotateX, rotateY, rotateZ);

                Console.WriteLine($"Clipboard has been rotated by Y: '{rotateY}', X: '{rotateX}', Z: '{rotateZ}' degrees!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /flip

        [Command("/flip")]
        private static void ExecuteFlip(string[] args)
        {
            // Ensure the copied clipboard is full.
            if (copiedRegion.Count() == 0)
            {
                Console.WriteLine("ERROR: You need to first copy a region.");
                return;
            }

            try
            {
                string flipDirectionInput = args.Length > 0 && !string.IsNullOrEmpty(args[0]) ? args[0] : "";

                // Check if the user wants to manually specify the flip direction. If not use facing direction.
                if (string.IsNullOrEmpty(flipDirectionInput))
                {
                    // Get the facing direction from the users location and the cursor location.
                    Direction facingDirection = GetFacingDirection(GetUsersLocation(), GetUsersCursorLocation());

                    // Perform the flip operation
                    FlipClipboard(facingDirection);

                    Console.WriteLine($"Clipboard has been flipped along '{facingDirection}'!");
                    return;
                }
                else if (Enum.TryParse(args[0], true, out Direction flipDirection))
                {
                    // Perform the flip operation
                    FlipClipboard(flipDirection);

                    Console.WriteLine($"Clipboard has been flipped along '{flipDirection}'!");
                    return;
                }

                Console.WriteLine($"ERROR: '{args[0]}' is not a valid direction. Use: (posX, negX, posZ, negZ, Up, Down)");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /clearclipboard

        [Command("/clearclipboard")]
        private static void ExecuteClearClipboard()
        {
            try
            {
                // Clear existing clearclipboard.
                ClearClipboard();

                Console.WriteLine($"Clipboard has been cleared!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Tool Commands.

        #region /tool

        [Command("/tool")]
        private void ExecuteTool(string[] args) // Don't give 'static' for tool command.
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /tool [on/off] [/command],\n" +
                                  "    /tool command [/command]"
                );
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        if (args.Length > 1) // Valid: 2+ 1-3+;
                        {
                            _toolCommand = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "/jump";
                            _toolItem = GetUsersHeldItem();

                            Timer toolTimer = new Timer() { Interval = 1 };
                            toolTimer.Tick += WorldTool_Tick;
                            toolTimer.Start();

                            _toolEnabled = true;
                            Console.WriteLine($"Tool Activated! Command: {_toolCommand}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing parameter. Usage: /tool [on/off] [/command]");
                            return;
                        }
                        break;

                    case "off":
                        if (args.Length > 0 && args.Length < 2) // Valid: 0+; 1-2.
                        {
                            _toolEnabled = false;
                            Console.WriteLine("Tool Deactivated!");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing parameter. Usage: /tool [on/off] [/command]");
                            return;
                        }
                        break;

                    case "command":
                        if (args.Length > 1) // Valid: 2+ 1-3+;
                        {
                            _toolCommand = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "/jump";
                            Console.WriteLine($"New Tool Command: {_toolCommand}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing parameter. Usage: /tool [on/off] [/command]");
                            return;
                        }
                        break;

                    default:
                        Console.WriteLine("ERROR: Command usage /tool [on/off] [/command]");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Brush Commands.

        #region /brush

        [Command("/brush")]
        [Command("/br")]
        private void ExecuteBrush(string[] args) // Don't give 'static' for tool command.
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: Command usage /brush [on/off] (block(,array)) (size),\n" +
                                  "    /brush block [block(,array)],\n" +
                                  "    /brush shape [shape],\n" +
                                  "    /brush size [size],\n" +
                                  "    /brush height [height],\n" +
                                  "    /brush hollow [true/false],\n" +
                                  "    /brush replace [true/false],\n" +
                                  "    /brush rapid [true/false]"
                );
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        if (args.Length > 0 && args.Length < 4) // Valid: 0+; 1-2.
                        {
                            string defaultBlock = !string.IsNullOrEmpty(_brushBlockPattern) ? _brushBlockPattern : "1";
                            string blockPattern = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : defaultBlock;
                            
                            // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                            blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                            // Make sure the input is within the min/max.
                            int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                            if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                            {
                                Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                                return;
                            }

                            _brushItem = GetUsersHeldItem();                                                   // Or use 'WandItemID'.
                            _brushBlockPattern = blockPattern;
                            _brushSize = args.Length > 2 && int.TryParse(args[2], out int s) ? s : _brushSize; // If value is not set, keep set value.

                            // Turn off brushing commands.
                            _brushReplaceMode = false;
                            _brushRapidMode = false;

                            Timer brushTimer = new Timer() { Interval = 1 };
                            brushTimer.Tick += WorldBrush_Tick;
                            brushTimer.Start();

                            _brushEnabled = true;
                            // Console.WriteLine($"Brush Activated! Block set to: {_brushBlockPattern}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing parameter. Usage: /brush [on/off] (block(,array)) (size)");
                            return;
                        }
                        break;

                    case "off":
                        Console.WriteLine("Brush Deactivated!");
                        _brushEnabled = false;
                        return;

                    case "block":
                        if (args.Length > 1 && args.Length < 3) // Valid: 0+; 1.
                        {
                            string defaultBlock = !string.IsNullOrEmpty(_brushBlockPattern) ? _brushBlockPattern : "1";
                            string blockPattern = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : defaultBlock;

                            // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                            blockPattern = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern);

                            // Make sure the input is within the min/max.
                            int[] blockPatternNumbers = (!string.IsNullOrEmpty(blockPattern)) ? blockPattern.Split(',').Select(int.Parse).ToArray() : new int[0];
                            if (blockPatternNumbers.Length == 0 || blockPatternNumbers.Min() < BlockIDValues.Item1 || blockPatternNumbers.Max() > BlockIDValues.Item2)
                            {
                                Console.WriteLine($"Block IDs are out of range. (min: {BlockIDValues.Item1}, max: {BlockIDValues.Item2})");
                                return;
                            }

                            _brushBlockPattern = blockPattern;
                            // Console.WriteLine($"Brush block set to: {_brushBlockPattern}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing block type. Usage: /brush block [block(,array)]");
                            return;
                        }
                        break;

                    case "shape":
                        if (args.Length > 1 && args.Length < 3 && IsValidBrushShape(args[1].ToLower())) // Valid: 0+; 1.
                        {
                            _brushShape = args[1].ToLower();
                            // Console.WriteLine($"Brush shape set to: {_brushShape}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Missing shape type. Usage: /brush shape [shape]");
                            return;
                        }
                        break;

                    case "size":
                        if (args.Length > 1 && args.Length < 3 && int.TryParse(args[1], out int size)) // Valid: 0+; 1.
                        {
                            _brushSize = size;
                            // Console.WriteLine($"Brush size set to: {_brushSize}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid brush size. Usage: /brush size [size]");
                            return;
                        }
                        break;

                    case "height":
                        if (args.Length > 1 && args.Length < 3 && int.TryParse(args[1], out int height)) // Valid: 0+; 1.
                        {
                            _brushHeight = height;
                            // Console.WriteLine($"Brush height set to: {_brushHeight}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid brush size. Usage: /brush height [height]");
                            return;
                        }
                        break;

                    case "hollow":
                        if (args.Length > 1 && args.Length < 3 && bool.TryParse(args[1], out bool hollow)) // Valid: 0+; 1.
                        {
                            _brushHollow = hollow;
                            // Console.WriteLine($"Brush hollow mode set to: {_brushHollow}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid replace mode. Usage: /brush hollow [true/false]");
                            return;
                        }
                        break;

                    case "replace":
                        if (args.Length > 1 && args.Length < 3 && bool.TryParse(args[1], out bool replaceMode)) // Valid: 0+; 1.
                        {
                            _brushReplaceMode = replaceMode;
                            // Console.WriteLine($"Brush replace mode set to: {_brushReplaceMode}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid replace mode. Usage: /brush replace [true/false]");
                            return;
                        }
                        break;

                    case "rapid":
                        if (args.Length > 1 && args.Length < 3 && bool.TryParse(args[1], out bool rapidMode)) // Valid: 0+; 1.
                        {
                            _brushRapidMode = rapidMode;
                            // Console.WriteLine($"Brush rapid mode set to: {_brushRapidMode}");
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid rapid mode. Usage: /brush rapid [true/false]");
                            return;
                        }
                        break;

                    default:
                        Console.WriteLine("ERROR: Command usage /brush [on/off] (block(,array)) (size),\n" +
                                  "    /brush block [block(,array)],\n" +
                                  "    /brush shape [shape],\n" +
                                  "    /brush size [size],\n" +
                                  "    /brush height [height],\n" +
                                  "    /brush hollow [true/false],\n" +
                                  "    /brush replace [true/false],\n" +
                                  "    /brush rapid [true/false]"
                        );
                        return;
                }

                // Display enabled message.
                Console.WriteLine($"Brush Activated!\n\n" +
                                  $"Block Type: {_brushBlockPattern}\n"+
                                  $"Block Shape: {_brushShape}\n" +
                                  $"Brush Size: {_brushSize}\n" +
                                  $"Brush Height: {_brushHeight}\n" +
                                  $"Hollow Mode: {_brushHollow}\n" +
                                  $"Replace Mode: {_brushReplaceMode}\n" +
                                  $"Rapid Mode: {_brushRapidMode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #endregion

        #region Timers

        #region World Wand

        private int _wandRunTimes;
        private void WorldWand_Tick(object sender, EventArgs e)
        {
            if (!IsNetworkSessionActive() || !_wandEnabled)
            {
                ((Timer)sender).Stop();
                _wandEnabled = false;
                return;
            }

            MouseState mouseState = Mouse.GetState();
            bool leftClick = mouseState.LeftButton == ButtonState.Pressed;
            bool rightClick = mouseState.RightButton == ButtonState.Pressed;

            if (GetUsersHeldItem() == WandItemID)
            {
                if (leftClick && _wandRunTimes == 0)
                {
                    _wandRunTimes++;
                    _pointToLocation1 = GetUsersCursorLocation();
                    Console.WriteLine("Position 1 " + _pointToLocation1 + " has been set!");
                }
                else if (!leftClick && !rightClick)
                {
                    _wandRunTimes = 0;
                }

                if (rightClick && _wandRunTimes == 0)
                {
                    _wandRunTimes++;
                    _pointToLocation2 = GetUsersCursorLocation();
                    Console.WriteLine("Position 2 " + _pointToLocation2 + " has been set!");
                }
            }
        }
        #endregion

        #region World Tool

        private int _toolRunTimes;
        private void WorldTool_Tick(object sender, EventArgs e)
        {
            if (!IsNetworkSessionActive() || !_toolEnabled)
            {
                ((Timer)sender).Stop();
                _toolEnabled = false;
                return;
            }

            MouseState mouseState = Mouse.GetState();
            bool leftClick = mouseState.LeftButton == ButtonState.Pressed;

            if (GetUsersHeldItem() == _toolItem)
            {
                if (leftClick && _toolRunTimes == 0)
                {
                    _toolRunTimes++;

                    string command = _toolCommand; // Define the command string.

                    // Split the command into method name and parameters.
                    string[] commandParts = command.Split(' ');

                    // Construct the method name (e.g., "ExecuteTest").
                    string methodName = "Execute" + commandParts[0].TrimStart('/').ToLower();

                    // Get the method expecting a string[] parameter using reflection, ignoring case.
                    // INFO: For running the tool's method from a static void use typeof(Program).
                    MethodInfo method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase);

                    if (method != null)
                    {
                        ParameterInfo[] methodParams = method.GetParameters();

                        if (methodParams.Length == 0)
                        {
                            // If the method has no parameters, invoke it directly.
                            method.Invoke(null, null);
                        }
                        else if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(string[]))
                        {
                            // Extract parameters from the command string (skip the command part, so start at index 1).
                            // .NET 8.0+ string[] parameters = commandParts.Length > 1 ? commandParts[1..] : new string[] { };
                            string[] parameters = new string[commandParts.Length - 1];
                            if (commandParts.Length > 1)
                            {
                                Array.Copy(commandParts, 1, parameters, 0, parameters.Length);
                            }

                            // Obsolete: This code is used to pass parameters to types.
                            // WorldEditCSharp passes parameters in a string argument.
                            /*
                                // Convert parameters to the appropriate types.
                                ParameterInfo[] methodParams = method.GetParameters();
                                object[] args = new object[methodParams.Length];
        
                                for (int i = 0; i < methodParams.Length; i++)
                                {
                                    if (i < parameters.Length)
                                    {
                                        // Convert the string argument to the appropriate parameter type.
                                        args[i] = Convert.ChangeType(parameters[i], methodParams[i].ParameterType);
                                    }
                                }

                                // Invoke the method with the converted arguments.
                                method.Invoke(null, args);
                            */

                            // Invoke the method with the parameters as a single string[] argument.
                            method.Invoke(null, new object[] { parameters });
                        }
                        else
                        {
                            Console.WriteLine($"The command '{command}' does not match any valid method signature.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"The command '{command}' not found.");
                    }
                }
                else if (!leftClick)
                {
                    _toolRunTimes = 0;
                }
            }
        }
        #endregion

        #region World Brush

        private int _brushRunTimes;       // Set default values.
        private string _brushBlockPattern = "1";
        private string _brushShape        = "sphere"; 
        private int _brushSize            = 4;
        private int _brushHeight          = 8;
        private bool _brushHollow         = false;
        private bool _brushReplaceMode    = false;
        private bool _brushRapidMode      = false;
        private void WorldBrush_Tick(object sender, EventArgs e)
        {
            if (!IsNetworkSessionActive() || !_brushEnabled)
            {
                ((Timer)sender).Stop();
                _brushEnabled = false;
                return;
            }

            MouseState mouseState = Mouse.GetState();
            bool leftClick = mouseState.LeftButton == ButtonState.Pressed;

            if (GetUsersHeldItem() == _brushItem) // Or use 'WandItemID'.
            {
                if (leftClick && (_brushRunTimes == 0 || _brushRapidMode))
                {
                    _brushRunTimes++;

                    // Define 1x1 region placeholder.
                    var cursorLocation = GetUsersCursorLocation();

                    // Define region placeholders.
                    var region = new HashSet<Tuple<Vector3, int>>();
                    var tempRegion = new HashSet<Vector3>(); // Temporary storage for Vector3-based regions.

                    // Check if the size is zero.
                    if (_brushSize == 0)
                    {
                        // Define 1x1 region.
                        tempRegion = new HashSet<Vector3>() { cursorLocation };
                    }
                    else
                    {
                        // Define the shape based on the users input.

                        // Get the center point.
                        Vector3 centerOffset = new Vector3(cursorLocation.X, cursorLocation.Y - (_brushSize / 2), cursorLocation.Z);
                        Vector3 buildLocation = (_brushReplaceMode) ? centerOffset  : centerOffset;

                        switch (_brushShape)
                        {
                            // Check if the from-block pattern contains air, and if so, have the region save it.
                            case "floor":
                                tempRegion = (_brushReplaceMode) ? MakeFloor(cursorLocation, _brushSize, _brushHollow, ignoreBlock: AirID) : MakeFloor(cursorLocation, _brushSize, _brushHollow);
                                break;

                            case "cube":
                                tempRegion = (_brushReplaceMode) ? MakeCube(buildLocation, _brushSize, _brushHollow, ignoreBlock: AirID) : MakeCube(buildLocation, _brushSize, _brushHollow);
                                break;

                            case "prism":
                                tempRegion = (_brushReplaceMode) ? MakeTriangularPrism(buildLocation, _brushSize, _brushSize, _brushHeight, _brushHollow, ignoreBlock: AirID) : MakeTriangularPrism(buildLocation, _brushSize, _brushSize, _brushHeight, _brushHollow);
                                break;

                            case "sphere":
                                tempRegion = (_brushReplaceMode) ? MakeSphere(buildLocation, _brushSize, _brushSize, _brushSize, _brushHollow, ignoreBlock: AirID) : MakeSphere(buildLocation, _brushSize, _brushSize, _brushSize, _brushHollow);
                                break;

                            case "ring":
                                tempRegion = (_brushReplaceMode) ? MakeRing(cursorLocation, _brushSize, _brushHollow, ignoreBlock: AirID) : MakeRing(cursorLocation, _brushSize, _brushHollow);
                                break;

                            case "pyramid":
                                tempRegion = (_brushReplaceMode) ? MakePyramid(buildLocation, _brushSize, _brushHollow, ignoreBlock: AirID) : MakePyramid(buildLocation, _brushSize, _brushHollow);
                                break;

                            case "cone":
                                tempRegion = (_brushReplaceMode) ? MakeCone(buildLocation, _brushSize, _brushSize, _brushHeight, _brushHollow, 1, ignoreBlock: AirID) : MakeCone(buildLocation, _brushSize, _brushSize, _brushHeight, _brushHollow, 1);
                                break;

                            case "cylinder":
                                tempRegion = (_brushReplaceMode) ? MakeCylinder(buildLocation, _brushSize, _brushSize, _brushHeight, _brushHollow, ignoreBlock: AirID) : MakeCylinder(buildLocation, _brushSize, _brushSize, _brushHeight, _brushHollow);
                                break;

                            case "diamond":
                                tempRegion = (_brushReplaceMode) ? MakeDiamond(buildLocation, _brushSize, _brushHollow, false, ignoreBlock: AirID) : MakeDiamond(buildLocation, _brushSize, _brushHollow, false);
                                break;

                            case "snow":
                                tempRegion = MakeSnow(cursorLocation, _brushSize, _brushReplaceMode);
                                break;

                            case "floodfill":
                                tempRegion = FloodFill(cursorLocation, _brushSize);
                                break;

                            case "tree":
                                region = MakeTree((int)cursorLocation.X, (int)cursorLocation.Z, _brushSize);
                                break;

                            case "schem":
                                if (copiedRegion.Count() == 0)
                                    Console.WriteLine("BRUSH: No schem data found. You need to first copy a region or import file.");
                                else
                                    region = PasteRegion(buildLocation);
                                break;

                            default:
                                tempRegion = (_brushReplaceMode) ? MakeSphere(buildLocation, _brushSize, _brushSize, _brushSize, _brushHollow, ignoreBlock: AirID) : MakeSphere(buildLocation, _brushSize, _brushSize, _brushSize, _brushHollow);
                                break;
                        }
                    }

                    // Convert tempRegion to region with Tuple<Vector3, int>.
                    foreach (var vec in tempRegion)
                    {
                        region.Add(new Tuple<Vector3, int>(vec, -1));
                    }

                    // Get the current block type from the cursor position.
                    int cursorBlock = (_brushReplaceMode) ? GetBlockFromLocation(GetUsersCursorLocation()) : -1; // Use -1 if off to save all.

                    // Save the existing region and clear the upcoming redo.
                    // If replacemode is enabled, do not save the entire region, only the effected blocks.
                    if (_brushReplaceMode)
                        SaveUndo(ExtractVector3HashSet(region), saveBlock: cursorBlock);
                    else
                        SaveUndo(ExtractVector3HashSet(region));
                    ClearRedo();

                    HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                    foreach (var i in region)
                    {
                        // Get random block from input.
                        int block;
                        if (i.Item2 == -1)
                            block = GetRandomBlockFromPattern(_brushBlockPattern);
                        else
                            block = i.Item2;

                        // Check for replace mode. If so, check if the current block is a block to replace, otherwise continue.
                        if (!_brushReplaceMode || (_brushReplaceMode && GetBlockFromLocation(i.Item1) == cursorBlock) || _brushShape == "snow")
                        {
                            // Place block if it doesn't already exist. (improves the performance)
                            if (GetBlockFromLocation(i.Item1) != block)
                            {
                                PlaceBlock(i.Item1, block);

                                // Add block to redo.
                                redoBuilder.Add(new Tuple<Vector3, int>(i.Item1, block));
                            }
                        }
                    }

                    // Save the actions to undo stack.
                    SaveUndo(redoBuilder);
                }
                else if (!leftClick)
                {
                    _brushRunTimes = 0;
                }
            }
        }
        #endregion

        #endregion

        /// <summary>
        /// REMOVE THIS IN TESTING!
        /// </summary>
        // Add constructors to evade compiling errors.
        private readonly CastleMinerZGame _game = CastleMinerZGame.Instance;
        public TextEditControl _textEditControl = new TextEditControl();
        public PlainChatInputScreen(bool drawBehind) : base(drawBehind)
        {
        }
        /// <summary>
        /// END OF REMOVAL.
        /// </summary>
    }
}