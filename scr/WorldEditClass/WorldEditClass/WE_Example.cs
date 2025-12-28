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
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Input;
using System.Reflection;
using System.Numerics;
using System.Linq;
using System.Text;
using System.IO;
using System;

using static WorldEdit.EnumMapper;
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

        private bool   _wandEnabled;
        private bool   _toolEnabled;
        private bool   _brushEnabled;

        private string _toolCommand = "";
        private int    _toolItem    = WandItemID; // Use the wand item as a placeholder.
        private int    _brushItem   = WandItemID; // Use the wand item as a placeholder.

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
            ("cc",                                                                     "Clears the chat. This was made for showcasing."),
            ("brightness [amount]",                                                    "Change the brightness. Use '1' for default. This was made for showcasing."),
            ("teleport [x] [y] [z]",                                                   "Teleport the player to a new position."),
            ("time [time]",                                                            "Change the worlds time. Use 0-100 for time of day."),
            ("toggleui",                                                               "Toggles the HUD and UI visibility."),

            // General Commands.
            ("help (page)",                                                            "Display all available commands."),
            ("undo (times)",                                                           "Undoes the last action (from history)."),
            ("redo (times)",                                                           "Redoes the last action (from history)."),
            ("clearhistory",                                                           "Clear your history."),

            // Navigation Commands.
            ("unstuck",                                                                "Escape from being stuck inside a block."),
            ("ascend (levels)",                                                        "Go up a floor."),
            ("descend (levels)",                                                       "Go down a floor."),
            ("ceil",                                                                   "Go to the ceiling."),
            ("thru",                                                                   "Pass through walls."),
            ("jumpto",                                                                 "Teleport to the cursors location."),
            ("up [amount]",                                                            "Go upwards some distance."),
            ("down [amount]",                                                          "Go downwards some distance."),

            // Selection Commands.
            ("pos [pos1/pos2..]",                                                      "Set positions."),
            ("hpos [hpos1/hpos2..]",                                                   "Set position to targeted block."),
            ("chunk (coordinates)",                                                    "Set the selection to your current chunk."),
            ("wand [on/off]",                                                          "Get the wand item."),
            ("contract [amount] (direction)",                                          "Contract the selection area."),
            ("shift [amount] (direction)",                                             "Shift the selection area."),
            ("trim [mask block(,array)]",                                              "Minimize the selection to encompass matching blocks."),
            ("size (clipboard)",                                                       "Get information about the selection."),
            ("count [find block(,array)]",                                             "Counts the number of blocks matching a mask."),
            ("distr (clipboard) (page)",                                               "Get the distribution of blocks in the selection."),
            ("expand [amount(vert)] (direction)",                                      "Expand the selection area."),

            // Region Commands.
            ("set [block(,array)] (hollow)",                                           "Sets all the blocks in the region."),
            ("break (mask block(,array))",                                             "Breaks all blocks in the region (drops items)."),
            ("line [block(,array)] (thickness)",                                       "Draws line segments between two positions."),
            ("replace [source block,(all)] [to block,(all)]",                          "Replace all blocks in the selection with another."),
            ("allexcept [source block(,array)] (to block(,array))",                    "Replace all blocks except a desired block pattern."),
            ("overlay [replace block(,array)]",                                        "Set a block on top of blocks in the region."),
            ("walls [block(,array)]",                                                  "Build the four sides of the selection."),
            ("smooth (iterations)",                                                    "Smooth the elevation in the selection."),
            ("move [amount] (direction)",                                              "Move the contents of the selection."),
            ("stack (amount) (direction) (useAir)",                                    "Repeat the contents of the selection."),
            ("stretch (amount) (direction) (useAir)",                                  "Stretch the contents of the selection."),
            ("spell [words(@linebreak)/(/paste)] [block(,array)] (flip) (rotate)",     "Draws a text made of blocks relative to position 1."),
            ("hollow (block(,array)) (thickness)",                                     "Hollows out the object contained in this selection."),
            ("fill [block(,array)]",                                                   "Fills only the inner-most blocks of an object contained in this selection."),
            ("wrap [replace block(,array)] (wrap direction(all)) (exclude direction)", "Fills only the outer-most air blocks of an object contained in this selection."),
            ("matrix [radius] [spacing] (snow) (default(,array))",                     "Places your clipboard spaced out in intervals."),
            ("forest [area_size] [density] (max_height)",                              "Make a forest within the region."),
            ("tree (max_height)",                                                      "Make a tree at position 1."),

            // Generation Commands.
            ("floor [block(,array)] [radius] (hollow)",                                "Makes a filled floor."),
            ("cube [block(,array)] [radii] (hollow)",                                  "Makes a filled cube."),
            ("prism [block(,array)] [length] [width] (height) (hollow)",               "Makes a filled prism."),
            ("sphere [block(,array)] [radii] (hollow) (height)",                       "Makes a filled sphere."),
            ("pyramid [block(,array)] [size] (hollow)",                                "Makes a filled pyramid."),
            ("cone [block(,array)] [radii] [height] (hollow)",                         "Makes a filled cone."),
            ("cylinder [block(,array)] [radii] [height] (hollow)",                     "Makes a filled cylinder."),
            ("diamond [r block(,array)] [radii] (hollow) (squared)",                   "Makes a filled diamond."),
            ("ring [block(,array)] [radius] (hollow)",                                 "Makes a filled ring."),
            ("ringarray [block(,array)] [amount] [space]",                             "Makes a hollowed ring at evenly spaced intervals."),
            ("generate [block(,array)] [expression(clipboard)] (hollow)",              "Generates a shape according to a formula."),

            // Schematic and Clipboard Commands.
            ("schematic [save] (saveAir)",                                             "Save your clipboard into a schematic file."),
            ("schematic [load] (loadAir)",                                             "Load a schematic into your clipboard."),
            ("copy",                                                                   "Copy the selection to the clipboard."),
            ("cut",                                                                    "Cut the selection to the clipboard."),
            ("paste (useAir) (pos1)",                                                  "Paste the clipboard's contents."),
            ("rotate (rotateY) (rotateX) (rotateZ)",                                   "Rotate the contents of the clipboard."),
            ("flip (direction)",                                                       "Flip the contents of the clipboard across the origin."),
            ("clearclipboard",                                                         "Clear your clipboard."),

            // Tool Commands.
            ("tool [on/off] [/command], "              +
                 "tool command [/command]",                                            "Binds a tool to the item in your hand."),

            // Brush Commands.
            ("brush [on/off] (block(,array)) (size), " +
                 "brush block [block(,array)], "       +
                 "brush shape [shape], "               +
                 "brush size [size], "                 +
                 "brush height [height], "             +
                 "brush hollow [true/false], "         +
                 "brush replace [true/false], "        +
                 "brush rapid [true/false]",                                           "Brushing commands."),

            // Utility Commands.
            ("removenear [radii] (pos1)",                                              "Remove all blocks within a cylindrical radii."),
            ("replacenear [radii] [source block,(all)] [to block,(all)] (pos1)",       "Replace all blocks within a cylindrical radii with another."),
            ("snow [block(,array)] [radius] (pos1)",                                   "Places a pattern of blocks on ground level.")
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
            if (args.Length == 0)
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
            if (args.Length == 0)
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

        #region /cui

        [Command("/cui")]
        private static void ExecuteCUI(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /cui [on/off]");
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        if (args.Length != 1) { Console.WriteLine("ERROR: Missing parameter. Usage: /cui [on/off]"); return; }

                        _enableCLU = true;
                        Console.WriteLine("Selections are now shown.");
                        break;

                    case "off":
                        if (args.Length != 1) { Console.WriteLine("ERROR: Missing parameter. Usage: /cui [on/off]"); return; }

                        _enableCLU = false;
                        Console.WriteLine("Selections are now hidden.");
                        break;

                    default:
                        Console.WriteLine("ERROR: Command usage /cui [on/off]");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

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

        #region /clearhistory

        [Command("/clearhistory")]
        [Command("/clearh")]
        private static void ExecuteClearHistory()
        {
            try
            {
                // Clear existing clearhistory.
                ClearHistory();

                Console.WriteLine($"History has been cleared!");
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
                        Console.WriteLine($"Stopped at level {levelCount}: No furthest valid location found.");
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
                        Console.WriteLine($"Stopped at level {levelCount}: No furthest valid location found.");
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
            if (args.Length == 0)
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
                if (upwardsLocation.Y <= WorldHeights.MaxY)
                {
                    PlaceBlock(upwardsLocation, 48);      // GlassMystery.
                    await Task.Delay(100);                // Add short wait.
                    upwardsLocation.Y += 1;               // Place user on top. (adjust for your user offset)
                    TeleportUser(upwardsLocation, false); // Teleport user.

                    Console.WriteLine($"Teleported up '{amount}' blocks!");
                }
                else
                    Console.WriteLine($"Location 'Y:{Math.Round(upwardsLocation.Y)}' is out of bounds. Max: '{WorldHeights.MaxY}'.");
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
            if (args.Length == 0)
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
                if (upwardsLocation.Y >= WorldHeights.MinY)
                {
                    PlaceBlock(upwardsLocation, 48);      // GlassMystery.
                    await Task.Delay(100);                // Add short wait.
                    upwardsLocation.Y += 1;               // Place user on top. (adjust for your user offset)
                    TeleportUser(upwardsLocation, false); // Teleport user.

                    Console.WriteLine($"Teleported down '{amount}' blocks!");
                }
                else
                    Console.WriteLine($"Location 'Y:{Math.Round(upwardsLocation.Y)}' is out of bounds. Max: '{WorldHeights.MinY}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        // Selection Commands.

        #region /pos

        [Command("/pos")]
        private static void ExecutePos(string[] args)
        {
            if (args.Length == 0)
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

        #region /hpos

        [Command("/hpos")]
        private static void ExecuteHpos(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /hpos [hpos1/hpos2..]");
                return;
            }

            try
            {
                int point = int.TryParse(args[0], out int p) ? p : 1;

                // Check what position to set.
                if (point == 1)
                    _pointToLocation1 = GetUsersCursorLocation();
                else if (point == 2)
                    _pointToLocation2 = GetUsersCursorLocation();

                // Ensure point is within range.
                if (point == 1 || point == 2)
                    Console.WriteLine($"Targeted position {point} ({(point == 1 ? $"{Math.Round(_pointToLocation1.X)}, {Math.Round(_pointToLocation1.Y)}, {Math.Round(_pointToLocation1.Z)}" : $"{Math.Round(_pointToLocation2.X)}, {Math.Round(_pointToLocation2.Y)}, {Math.Round(_pointToLocation2.Z)}")}) has been set!");
                else
                    Console.WriteLine($"Targeted position {point} is not valid!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /chunk

        [Command("/chunk")]
        private static void ExecuteChunk(string[] args)
        {
            // Decide which point to center on.
            Vector3 chunkLoc = Vector3.Zero;
            bool useArg = args.Length == 1 && TryParseXYZ(args[0], out chunkLoc);

            // If no chunk location was specified or valid, fall back to the users location.
            if (!useArg)
                chunkLoc = GetUsersLocation();

            // Compute chunk indices using mathematical floor.
            int sizeX = ChunkSize.WidthX;
            int sizeZ = ChunkSize.LengthZ;

            int chunkX = FloorDiv((int)Math.Floor(chunkLoc.X), sizeX);
            int chunkZ = FloorDiv((int)Math.Floor(chunkLoc.Z), sizeZ);

            // Corner of the chunk (lower X/Z).
            int minX = chunkX * sizeX;
            int minZ = chunkZ * sizeZ;

            // Opposite corner (inclusive).
            int maxX = minX + sizeX - 1;
            int maxZ = minZ + sizeZ - 1;

            // Update selection - full vertical column.
            _pointToLocation1 = new Vector3(minX, WorldHeights.MinY, minZ);
            _pointToLocation2 = new Vector3(maxX, WorldHeights.MaxY, maxZ);

            // Feedback.
            Console.WriteLine(
                $"Chunk selected at X:{chunkX} Z:{chunkZ}  " +
                $"({minX},{WorldHeights.MinY},{minZ}) -> ({maxX},{WorldHeights.MaxY},{maxZ})");
        }
        #endregion

        #region /wand

        [Command("/wand")]
        private void ExecuteWand(string[] args) // Don't give 'static' for wand command.
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /wand [on/off]");
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "on":
                        if (args.Length != 1) { Console.WriteLine("ERROR: Missing parameter. Usage: /wand [on/off]"); return; }

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
                        break;

                    case "off":
                        if (args.Length != 1) { Console.WriteLine("ERROR: Missing parameter. Usage: /wand [on/off]"); return; }

                        _wandEnabled = false;
                        Console.WriteLine("Wand Deactivated!");
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

        #region /contract
        
        [Command("/contract")]
        private static void ExecuteContract(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /contract [amount] (direction)");
                return;
            }

            try
            {
                if (!int.TryParse(args[0], out int amount) || amount < 0)
                {
                    Console.WriteLine("ERROR: [amount] must be a positive integer.");
                    return;
                }

                // Parse optional direction (null ? all faces).
                Direction? direction = null;
                if (args.Length >= 2 &&
                    !string.IsNullOrWhiteSpace(args[1]) &&
                    !args[1].Equals("all", StringComparison.OrdinalIgnoreCase) && // allow  "all".
                    Enum.TryParse(args[1], true, out Direction parsedDir))
                {
                    direction = parsedDir;
                }

                // Work with *min* and *max* so we don't care which corner is pos1/pos2.
                Vector3 min = Vector3.Min(_pointToLocation1, _pointToLocation2);
                Vector3 max = Vector3.Max(_pointToLocation1, _pointToLocation2);

                void shrink(ref float face, int by) => face -= by;
                void grow  (ref float face, int by) => face += by;

                if (direction is null) // contract from every face.
                {
                    grow  (ref min.X, amount); shrink(ref max.X, amount);
                    grow  (ref min.Y, amount); shrink(ref max.Y, amount);
                    grow  (ref min.Z, amount); shrink(ref max.Z, amount);
                }
                else                   // contract one face only.
                {
                    switch (direction.Value)
                    {
                        case Direction.posX: shrink(ref max.X, amount); break;
                        case Direction.negX: grow  (ref min.X, amount); break;
                        case Direction.Up:   shrink(ref max.Y, amount); break;
                        case Direction.Down: grow  (ref min.Y, amount); break;
                        case Direction.posZ: shrink(ref max.Z, amount); break;
                        case Direction.negZ: grow  (ref min.Z, amount); break;
                    }
                }

                // Validate that we still have a non-negative region.
                if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
                {
                    Console.WriteLine("ERROR: Contract amount too large  selection would invert/vanish.");
                    return;
                }

                // Ensure the expansion does not exceed world height limits.
                // This should not be an issue for contracting.
                ClampToWorldHeight(ref min.Y, ref max.Y);

                // Persist the new selection back to the global points.
                _pointToLocation1 = min;
                _pointToLocation2 = max;

                // Feedback.
                string dirText = direction?.ToString() ?? "all faces";
                Console.WriteLine(
                    $"Contracted selection by {amount} block(s) on {dirText}.");
                    // $"New region: ({Math.Round(min.X)}, {Math.Round(min.Y)}, {Math.Round(min.Z)}) -> " +
                    // $"({Math.Round(max.X)}, {Math.Round(max.Y)}, {Math.Round(max.Z)}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /shift

        [Command("/shift")]
        private static void ExecuteShift(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /shift [amount] (direction)");
                return;
            }

            try
            {
                if (!int.TryParse(args[0], out int amount) || amount < 0)
                {
                    Console.WriteLine("ERROR: [amount] must be a positive integer.");
                    return;
                }

                // Parse direction. Use facing direction as the default. 
                Vector3 cursorLocation = GetUsersCursorLocation();
                Direction dir = GetFacingDirection(_pointToLocation1, cursorLocation);
                if (args.Length >= 2 &&
                    !string.IsNullOrWhiteSpace(args[1]) &&
                    Enum.TryParse(args[1], true, out Direction parsedDir))
                {
                    dir = parsedDir;
                }

                // Build the offset vector.
                Vector3 offset;
                switch (dir)
                {
                    case Direction.posX:
                        offset = new Vector3(+amount, 0, 0);
                        break;
                    case Direction.negX:
                        offset = new Vector3(-amount, 0, 0);
                        break;
                    case Direction.posZ:
                        offset = new Vector3(0, 0, +amount);
                        break;
                    case Direction.negZ:
                        offset = new Vector3(0, 0, -amount);
                        break;
                    case Direction.Up:
                        offset = new Vector3(0, +amount, 0);
                        break;
                    case Direction.Down:
                        offset = new Vector3(0, -amount, 0);
                        break;
                    default:
                        offset = Vector3.Zero;
                        break;
                }

                // C# 8.0+.
                /*
                Vector3 offset = dir switch
                {
                    Direction.posX => new Vector3(+amount, 0, 0),
                    Direction.negX => new Vector3(-amount, 0, 0),
                    Direction.posZ => new Vector3(0, 0, +amount),
                    Direction.negZ => new Vector3(0, 0, -amount),
                    Direction.Up   => new Vector3(0, +amount, 0),
                    Direction.Down => new Vector3(0, -amount, 0),
                    _              => Vector3.Zero
                };
                */

                // Compute new positions.
                Vector3 newP1 = _pointToLocation1 + offset;
                Vector3 newP2 = _pointToLocation2 + offset;

                // Keep X & Z unlimited; clamp Y only.
                newP1.Y = Clamp((int)newP1.Y, WorldHeights.MinY, WorldHeights.MaxY);
                newP2.Y = Clamp((int)newP2.Y, WorldHeights.MinY, WorldHeights.MaxY);

                // If clamp collapsed the region completely (i.e. it would lie outside).
                if (newP1.Y == newP2.Y && _pointToLocation1.Y != _pointToLocation2.Y)
                {
                    Console.WriteLine("ERROR: Shift would move the selection outside world height limits.");
                    return;
                }

                // Persist & report.
                _pointToLocation1 = newP1;
                _pointToLocation2 = newP2;

                Console.WriteLine(
                    $"Shifted selection {amount} block(s) toward {dir}.");
                    // $"New region: ({Math.Round(newP1.X)}, {Math.Round(newP1.Y)}, {Math.Round(newP1.Z)}) -> " +
                    // $"({Math.Round(newP2.X)}, {Math.Round(newP2.Y)}, {Math.Round(newP2.Z)}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /trim

        [Command("/trim")]
        private static void ExecuteTrim(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /trim [mask block(,array)]");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] maskArray = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (maskArray.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data and mask data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);
                HashSet<int> maskSet = new HashSet<int>(maskArray);

                // TrimRegion(Region region, HashSet<int> maskSet, out Vector3 outMin, out Vector3 outMax).
                // Find bounding box of matching blocks.
                if (!TrimRegion(definedRegion, maskSet, out Vector3 newMin, out Vector3 newMax))
                {
                    Console.WriteLine("No matching blocks found inside the current selection.");
                    return;
                }

                // Clamp Y to world height limits just in case.
                newMin.Y = Math.Max(newMin.Y, WorldHeights.MinY);
                newMax.Y = Math.Min(newMax.Y, WorldHeights.MaxY);

                // Persist & feedback.
                _pointToLocation1 = newMin;
                _pointToLocation2 = newMax;

                Console.WriteLine(
                    $"Trimmed selection to matching blocks.");
                    // $"New region: ({Math.Round(newMin.X)}, {Math.Round(newMin.Y)}, {Math.Round(newMin.Z)}) -> " +
                    // $"({Math.Round(newMax.X)}, {Math.Round(newMax.Y)}, {Math.Round(newMax.Z)}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /size

        [Command("/size")]
        private static void ExecuteSize(string[] args)
        {
            bool fromClipboard = args.Length >= 1 && args[0].Equals("clipboard", StringComparison.OrdinalIgnoreCase);

            // If clipboard was requested and empty, display an error and return.
            if (fromClipboard && (copiedRegion == null || copiedRegion.Count == 0))
            {
                Console.WriteLine("ERROR: The clipboard is empty. Try using /copy first.");
                return;
            }

            // Decide which bounding box to use.
            if (!(fromClipboard && GetBoundingBoxFromRegion(out Vector3 min, out Vector3 max)))
            {
                // Make sure both points have been set.
                if (_pointToLocation1 == default || _pointToLocation2 == default)
                {
                    Console.WriteLine("ERROR: You must set /pos1 and /pos2 first.");
                    return;
                }

                // Put the two corners into canonical order.
                min = Vector3.Min(_pointToLocation1, _pointToLocation2);
                max = Vector3.Max(_pointToLocation1, _pointToLocation2);
            }

            // Calculate dimensions (inclusive, so +1).
            int sizeX = (int)(max.X - min.X) + 1;
            int sizeY = (int)(max.Y - min.Y) + 1;
            int sizeZ = (int)(max.Z - min.Z) + 1;

            long volume = (long)sizeX * sizeY * sizeZ;

            // Generate a neat summary. // C# 11+ can use raw string '"""'.
            string report = new StringBuilder()
                .AppendLine($"----------------------------------------------------")
                .AppendLine(
                    $"Selection corners : ({Math.Round(min.X)}, {Math.Round(min.Y)}, {Math.Round(min.Z)}) -> " +
                    $"({Math.Round(max.X)}, {Math.Round(max.Y)}, {Math.Round(max.Z)})"
                )
                .AppendLine($"Width  (X)        : {sizeX} block{(sizeX == 1 ? "" : "s")}")
                .AppendLine($"Height (Y)        : {sizeY} block{(sizeY == 1 ? "" : "s")}")
                .AppendLine($"Length (Z)        : {sizeZ} block{(sizeZ == 1 ? "" : "s")}")
                .AppendLine($"Total volume      : {volume:N0} block{(volume == 1 ? "" : "s")}")
                .Append    ($"----------------------------------------------------")
                .ToString();

            // Display report.
            Console.WriteLine(report);
        }
        #endregion

        #region /count

        [Command("/count")]
        private static void ExecuteCount(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /count [find block(,array)]");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] maskArray = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (maskArray.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data and mask data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);
                HashSet<int> maskSet = new HashSet<int>(maskArray);

                // Decide if air blocks (denoted by AirID) should be ignored.
                // For example, if the input does NOT include AirID, then we ignore air in the region.
                int ignoreBlock = (!maskSet.Contains(AirID)) ? AirID : -1;

                // CountRegion(Region region, HashSet<int> maskSet, int ignoreBlock = -1).
                // Pass the HashSet of block IDs (converted from the array) to CountRegion, along with the ignore block.
                var regionBlocks = CountRegion(definedRegion, maskSet, ignoreBlock);

                // Group the blocks by block type (Item2 in the tuple) and count them.
                var blockCounts = regionBlocks
                    .GroupBy(t => t.Item2)
                    .Select(g => new { BlockType = g.Key, Count = g.Count() });

                // Output the results, displaying each unique block's count on a new line.
                if (regionBlocks.Count > 0)
                {
                    Console.WriteLine("Blocks found matching the criteria:");
                    foreach (var block in blockCounts)
                    {
                        Console.WriteLine($"Block ID {block.BlockType}: {block.Count}");
                    }
                }
                else
                    Console.WriteLine($"{regionBlocks.Count} blocks found matching the criteria.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        #endregion

        #region /distr
        
        [Command("/distr")]
        private static void ExecuteDistr(string[] args)
        {
            bool fromClipboard = false;
            int  page          = 1;
        
            if (args.Length == 1)
            {
                if (args[0].Equals("clipboard", StringComparison.OrdinalIgnoreCase))
                    fromClipboard = true;
                else if (!int.TryParse(args[0], out page) || page < 1)
                {
                    Console.WriteLine("ERROR: Page must be a positive integer.");
                    return;
                }
            }
            else if (args.Length >= 2)
            {
                if (!args[0].Equals("clipboard", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("ERROR: Command usage /distr (clipboard) (page)");
                    return;
                }
                fromClipboard = true;
        
                if (!int.TryParse(args[1], out page) || page < 1)
                {
                    Console.WriteLine("ERROR: Page must be a positive integer.");
                    return;
                }
            }
        
            // Get IDs: Clipboard or live selection.
            IEnumerable<int> idStream;
        
            if (fromClipboard)
            {
                if (copiedRegion == null || copiedRegion.Count == 0)
                {
                    Console.WriteLine("ERROR: The clipboard is empty. Try using /copy first.");
                    return;
                }
                idStream = copiedRegion.Select(t => t.Item2);
            }
            else
            {
                if (_pointToLocation1 == default || _pointToLocation2 == default)
                {
                    Console.WriteLine("ERROR: You must set /pos1 and /pos2 first.");
                    return;
                }
        
                Vector3 min = Vector3.Min(_pointToLocation1, _pointToLocation2);
                Vector3 max = Vector3.Max(_pointToLocation1, _pointToLocation2);
                idStream    = EnumerateIdsInRegion(min, max);
            }
        
            // Count occurrences.
            var counts = new Dictionary<int, long>();
            long total = 0;
        
            foreach (int id in idStream)
            {
                counts[id] = counts.TryGetValue(id, out long c) ? c + 1 : 1;
                total++;
            }
        
            if (total == 0)
            {
                Console.WriteLine("Nothing to count: the chosen region is empty.");
                return;
            }
        
            // Paging.
            const int rowsPerPage = 4;
            int totalPages = (counts.Count + rowsPerPage - 1) / rowsPerPage;
        
            if (page > totalPages)
            {
                Console.WriteLine($"ERROR: Page {page} is out of range (max {totalPages}).");
                return;
            }
        
            var ordered = counts.OrderByDescending(p => p.Value)
                                .Skip((page - 1) * rowsPerPage)
                                .Take(rowsPerPage);
        
            // Build report.
            var sb = new StringBuilder();
            sb.AppendLine("----------------------------------------------------");
            sb.AppendLine($"Block distribution ({total:N0} blocks total) | Page {page}/{totalPages}");
            sb.AppendLine("ID / Name                   Count      Share");
            sb.AppendLine("----------------------------------------------------");
        
            foreach (var pair in ordered)
            {
                int   id    = pair.Key;
                long  count = pair.Value;
                double pct  = (double)count / total;
        
                string name = Enum.IsDefined(typeof(DNA.CastleMinerZ.Terrain.BlockTypeEnum), id)
                            ? Enum.GetName(typeof(DNA.CastleMinerZ.Terrain.BlockTypeEnum), id)
                            : "Unknown";
        
                sb.AppendLine($"{id,3} {name,-18} {count,10:N0}    {pct,7:P2}");
            }
        
            sb.Append("----------------------------------------------------");
            Console.WriteLine(sb.ToString());
        }
        #endregion

        #region /expand
        
        [Command("/expand")]
        private static void ExecuteExpand(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /expand [amount(vert)] (direction)");
                return;
            }
        
            try
            {
                // Check for the vertical keyword. If so, only vertically expand the selection to world limits.
                if (args[0].Equals("vert", StringComparison.OrdinalIgnoreCase))
                {
                    Vector3 minVert = Vector3.Min(_pointToLocation1, _pointToLocation2);
                    Vector3 maxVert = Vector3.Max(_pointToLocation1, _pointToLocation2);
            
                    minVert.Y = WorldHeights.MinY;
                    maxVert.Y = WorldHeights.MaxY;
            
                    _pointToLocation1 = minVert;
                    _pointToLocation2 = maxVert;
            
                    Console.WriteLine(
                        $"Expanded selection vertically to world limits ({WorldHeights.MinY} ? {WorldHeights.MaxY}).");
                    return;
                }
    
                if (!int.TryParse(args[0], out int amount) || amount < 0)
                {
                    Console.WriteLine("ERROR: [amount] must be a positive integer.");
                    return;
                }

                // Parse optional direction (null ? all faces).
                Direction? direction = null;
                if (args.Length >= 2 &&
                    !string.IsNullOrWhiteSpace(args[1]) &&
                    !args[1].Equals("all", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse(args[1], true, out Direction parsedDir))
                {
                    direction = parsedDir;
                }

                // Work with *min* and *max* so we don't care which corner was set first.
                Vector3 min = Vector3.Min(_pointToLocation1, _pointToLocation2);
                Vector3 max = Vector3.Max(_pointToLocation1, _pointToLocation2);

                void growMin (ref float face, int by) => face -= by;
                void growMax (ref float face, int by) => face += by;

                if (direction is null) // expand every face.
                {
                    growMin(ref min.X, amount); growMax(ref max.X, amount);
                    growMin(ref min.Y, amount); growMax(ref max.Y, amount);
                    growMin(ref min.Z, amount); growMax(ref max.Z, amount);
                }
                else                   // expand one face only.
                {
                    switch (direction.Value)
                    {
                        case Direction.posX: growMax(ref max.X, amount); break;
                        case Direction.negX: growMin(ref min.X, amount); break;
                        case Direction.Up:   growMax(ref max.Y, amount); break;
                        case Direction.Down: growMin(ref min.Y, amount); break;
                        case Direction.posZ: growMax(ref max.Z, amount); break;
                        case Direction.negZ: growMin(ref min.Z, amount); break;
                    }
                }

                // Ensure the expansion does not exceed world height limits.
                ClampToWorldHeight(ref min.Y, ref max.Y);

                // Persist the new selection back to the global points.
                _pointToLocation1 = min;
                _pointToLocation2 = max;

                // Feedback.
                string dirText = direction?.ToString() ?? "all faces";
                Console.WriteLine(
                    $"Expanded selection by {amount} block(s) on {dirText}.");
                    // $"New region: ({Math.Round(min.X)}, {Math.Round(min.Y)}, {Math.Round(min.Z)}) -> " +
                    // $"({Math.Round(max.X)}, {Math.Round(max.Y)}, {Math.Round(max.Z)}).");
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
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /set [block(,array)] (hollow)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                bool hollow = args.Length > 1 && args[1].Equals("true", StringComparison.OrdinalIgnoreCase);

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

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
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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

        #region /line

        [Command("/line")]
        private static void ExecuteLine(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /line [block(,array)] (thickness)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int thickness = args.Length > 1 && int.TryParse(args[1], out int t) ? t : 0;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // MakeLine(Region region, int thickness).
                var region = MakeLine(definedRegion, thickness);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] searchPatternNumbers = (searchPattern == "all") ? Array.Empty<int>() : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(searchPattern, BlockIDValues);
                if (searchPattern != "all" && searchPatternNumbers.Length == 0) return;   // Make sure the input is within the min/max.
                int[] replacePatternNumbers = (replacePattern == "all") ? Array.Empty<int>() : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern, BlockIDValues);
                if (replacePattern != "all" && replacePatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Use fill region to define a rectangular area to search in.
                // FillRegion(Region region, bool hollow, int ignoreBlock = -1).
                var region = FillRegion(definedRegion, false);

                // Save the existing region and clear the upcoming redo.
                if (searchPattern == "all")
                    SaveUndo(region);
                else
                    SaveUndo(region, saveBlock: searchPatternNumbers);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get the current block type.
                    int currentBlock = GetBlockFromLocation(blockLocation);

                    // Check if the current block is a block to replace.
                    if ((searchPattern == "all" && currentBlock != AirID) || searchPatternNumbers.Contains(currentBlock)) // Make sure not to replace 'air' when using 'all' mode.
                    {
                        // Get random block from input.
                        HashSet<int> excludedBlocks = new HashSet<int> { AirID, 26 }; // IDs to exclude. Block ID 26 'Torch' crashes.
                        int replaceBlock = (replacePattern == "all") ? GetRandomBlock(excludedBlocks) : GetRandomBlockFromPattern(replacePatternNumbers);

                        // Place block if it doesn't already exist. (improves the performance).
                        if (GetBlockFromLocation(blockLocation) != replaceBlock)
                        {
                            PlaceBlock(blockLocation, replaceBlock);

                            // Add block to redo.
                            redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, replaceBlock));
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
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /allexcept [source block(,array)] (to block(,array))");
                return;
            }

            try
            {
                string exceptPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                string replacePattern = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : "0";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] exceptPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(exceptPattern);
                if (exceptPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                int[] replacePatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern, BlockIDValues);
                if (replacePatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Use fill region to define a rectangular area to search in.
                // FillRegion(Region region, bool hollow, int ignoreBlock = -1).
                var region = FillRegion(definedRegion, false);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get the current block type.
                    int currentBlock = GetBlockFromLocation(blockLocation);

                    // Convert string to a list of integers.
                    var excludedBlocks = exceptPattern.Split(',').Select(int.Parse).ToList();

                    // Check if the current block is not excluded, and its not air, place new block.
                    if ((!excludedBlocks.Contains(currentBlock)) && currentBlock != AirID)
                    {
                        // Get random block from input.
                        int replaceBlock = GetRandomBlockFromPattern(replacePatternNumbers);

                        // Place block if it doesn't already exist. (improves the performance).
                        if (GetBlockFromLocation(blockLocation) != replaceBlock)
                        {
                            PlaceBlock(blockLocation, replaceBlock);

                            // Add block to redo.
                            redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, replaceBlock));
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

        #region /overlay

        [Command("/overlay")]
        private static void ExecuteOverlay(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /overlay [replace block(,array)]");
                return;
            }

            try
            {
                string replacePattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "0";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] replacePatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern, BlockIDValues);
                if (replacePatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // OverlayObject(Region region, List<int> replaceBlockPattern).
                var replaceBlockPattern = replacePattern.Split(',').Select(int.Parse).ToList();
                var region = OverlayObject(definedRegion, replaceBlockPattern);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(replacePatternNumbers);

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

        #region /walls

        [Command("/walls")]
        private static void ExecuteWalls(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /walls [block(,array)]");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";

               // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // MakeWalls(Region region).
                var region = MakeWalls(definedRegion);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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

        #region /move

        [Command("/move")]
        private static void ExecuteMove(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /move [amount] (direction)");
                return;
            }

            try
            {
                int moveAmount = int.TryParse(args[0], out int m) ? m : 1;

                // Check if the move amount is greater then zero.
                if (moveAmount < 1)
                {
                    Console.WriteLine($"ERROR: 'Amount' must be greater then '0'!");
                    return;
                }

                // Default settings.
                Direction moveDirection = Direction.posX;

                // If only amount is provided, derive direction from the user's cursor.
                if (args.Length == 1)
                {
                    Vector3 cursorLocation = GetUsersCursorLocation();
                    moveDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                }
                // Otherwise, try to parse the given direction.
                else if (args.Length >= 2)
                {
                    if (!Enum.TryParse<Direction>(args[1], true, out moveDirection))
                    {
                        // Fallback if parsing fails.
                        Vector3 cursorLocation = GetUsersCursorLocation();
                        moveDirection = GetFacingDirection(_pointToLocation1, cursorLocation);
                    }
                }

                // Define the region to move using the two global points.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // Calculate the move offset based on the direction and amount.
                Vector3 moveOffset = GetDirectionalUnitOffset(moveDirection) * moveAmount;

                // MoveRegion(Region region, Vector3 moveOffset).
                var originalBlocks = MoveRegion(definedRegion, moveOffset);
                
                SaveUndo(ExtractVector3HashSet(originalBlocks));
                ClearRedo();

                // Iterate over each block and perform the move.
                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var i in originalBlocks)
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

                SaveUndo(redoBuilder);
                Console.WriteLine($"{originalBlocks.Count / 2} blocks have been moved!");
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
                        // If the direction string doesn't match, fallback to the cursor direction.
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
                        // If the direction string doesn't match, fallback to the cursor direction.
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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeWords(Vector3 pos, string wordString, bool flipAxes = false, bool rotate90 = false).
                var region = MakeWords(_pointToLocation1, words, flipAxis, rotate90);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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

        #region /hollow

        [Command("/hollow")]
        private static void ExecuteHollow(string[] args)
        {
            try
            {
                string replacePattern = args.Length > 0 && !string.IsNullOrEmpty(args[0]) ? args[0] : "0";
                int thickness = args.Length > 1 && int.TryParse(args[1], out int t) ? t : 1;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] replacePatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern, BlockIDValues);
                if (replacePatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // HollowObject(Region region, int thickness).
                var replaceBlockPattern = replacePattern.Split(',').Select(int.Parse).ToList();
                var region = HollowObject(definedRegion, thickness);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(replacePatternNumbers);

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

        #region /fill

        [Command("/fill")]
        private static void ExecuteFill(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /fill [block(,array)]");
                return;
            }

            try
            {
                string replacePattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] replacePatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern, BlockIDValues);
                if (replacePatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // FillHollowObject(Region region).
                var replaceBlockPattern = replacePattern.Split(',').Select(int.Parse).ToList();
                var region = FillHollowObject(definedRegion);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(replacePatternNumbers);

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

        #region /wrap

        [Command("/wrap")]
        private static void ExecuteWrap(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /wrap [replace block(,array)] (wrap direction(all)) (exclude direction)");
                return;
            }

            try
            {
                string replacePattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "0";

                // Default settings.
                Direction? wrapDirection = null;
                Direction? excludeDirection = null;

                if (args.Length >= 2 && !string.IsNullOrEmpty(args[1]) && args[1] != "all")
                {
                    if (Enum.TryParse<Direction>(args[1], true, out var parsedWrap))
                        wrapDirection = parsedWrap;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid wrap direction specified.");
                        return;
                    }
                }
                if (args.Length >= 3 && !string.IsNullOrEmpty(args[2]))
                {
                    if (Enum.TryParse<Direction>(args[2], true, out var parsedExclude))
                        excludeDirection = parsedExclude;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid wrap direction specified.");
                        return;
                    }
                }

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] replacePatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern, BlockIDValues);
                if (replacePatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // WrapObject(Region region, List<int> replaceBlockPattern, Direction? wrapDirection = null, Direction? excludeDirection = null).
                var replaceBlockPattern = replacePattern.Split(',').Select(int.Parse).ToList();
                var region = WrapObject(definedRegion, replaceBlockPattern, wrapDirection, excludeDirection);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(replacePatternNumbers);

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

                // Populate optional arguments.
                for (int i = 2; i < args.Length; i++)
                {
                    var a = args[i].Trim();

                    // Check if it's parseable as a bool.
                    if (bool.TryParse(a, out bool parsedSnow))
                    {
                        snow = parsedSnow;
                    }
                    else
                    {
                        // Otherwise, assume it's the block pattern.
                        optionalBlockPattern = a;
                    }
                }

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] optionalBlockPatternNumbers = (!string.IsNullOrEmpty(optionalBlockPattern)) ? GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(optionalBlockPattern) : new int[0];
                // if (optionalBlockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeMatrix(Vector3 pos, int radius, int spacing, bool enableSnow, int[] optionalBlockPattern).
                var region = MakeMatrix(_pointToLocation1, radius, spacing, snow, optionalBlockPatternNumbers);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var (blockLocation, block) in region)
                {
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

        #region /forest

        [Command("/forest")]
        private static void ExecuteForest(string[] args)
        {
            if (args.Length == 0)
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
                foreach (var (blockLocation, block) in region)
                {
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
                foreach (var (blockLocation, block) in region)
                {
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

        #region /break

        [Command("/break")]
        private static void ExecuteBreak(string[] args)
        {
            try
            {
                string blockPattern = args.Length > 0 && !string.IsNullOrEmpty(args[0]) ? args[0] : "";

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] maskArray = (!string.IsNullOrEmpty(blockPattern)) ? GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues) : new int[0];
                // if (maskArray.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);
                HashSet<int> maskSet = new HashSet<int>(maskArray);

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
                var region = FillRegion(definedRegion, false, AirID);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Check if the mask is enabled. If not, remove block, if so, use mask.
                    int block = GetBlockFromLocation(blockLocation);
                    if (string.IsNullOrEmpty(blockPattern) || maskSet.Contains(block))
                    {
                        PlaceBlock(blockLocation, AirID);

                        // Try to map the block to its item id. If valid, drop the block as an item.
                        var blockType = (DNA.CastleMinerZ.Terrain.BlockTypeEnum)block;
                        if (TryMapEnum<DNA.CastleMinerZ.Terrain.BlockTypeEnum, DNA.CastleMinerZ.Inventory.InventoryItemIDs>(blockType, out var item))
                            DropItem(blockLocation, (int)item);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, AirID));
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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeFloor(Vector3 pos, int size, bool hollow, int ignoreBlock = -1).
                var region = MakeFloor(_pointToLocation1, radius, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeCube(Vector3 pos, int radii, bool hollow, int ignoreBlock = -1).
                var region = MakeCube(_pointToLocation1, radii, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeTriangularPrism(Vector3 pos, int length, int width, int height, bool hollow, int ignoreBlock = -1).
                var region = MakeTriangularPrism(_pointToLocation1, length, width, height, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeSphere(Vector3 pos, double radiusX, double radiusY, double radiusZ, bool hollow, int ignoreBlock = -1).
                var region = MakeSphere(_pointToLocation1, radii, height, radii, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakePyramid(Vector3 pos, int size, bool hollow, int ignoreBlock = -1).
                var region = MakePyramid(_pointToLocation1, size, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeCone(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, double thickness, int ignoreBlock = -1).
                var region = MakeCone(_pointToLocation1, radii, radii, height, hollow, 1);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeCylinder(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, int ignoreBlock = -1).
                var region = MakeCylinder(_pointToLocation1, radii, radii, height, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeDiamond(Vector3 pos, int size, bool hollow, int ignoreBlock = -1).
                var region = MakeDiamond(_pointToLocation1, radii, hollow, squared);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeRing(Vector3 pos, double radius, bool hollow, int ignoreBlock = -1).
                var region = MakeRing(_pointToLocation1, radius, hollow);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeRing(Vector3 pos, double radius, bool hollow, int ignoreBlock = -1).
                HashSet<Vector3> region = new HashSet<Vector3>();
                for (int i = 0; i < amount; i++)
                    region.UnionWith(MakeRing(_pointToLocation1, i * space, true));

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Define location data.
                Region definedRegion = new Region(_pointToLocation1, _pointToLocation2);

                // MakeShape(Vector3 pos, string expression, bool hollow, int ignoreBlock = -1).
                var region = MakeShape(definedRegion, expression, hollow);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var (blockLocation, block) in region)
                {
                    // Check if output is -1, use random block from input.
                    int blockToUse = block;
                    if (block == -1)
                        blockToUse = GetRandomBlockFromPattern(blockPatternNumbers);
                    else
                        if (blockToUse < BlockIDValues.MinID || blockToUse > BlockIDValues.MaxID) // Ensure the returning value is valid.
                            blockToUse = GetRandomBlockFromPattern(blockPatternNumbers);

                    // Place block if it doesn't already exist. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != blockToUse)
                    {
                        PlaceBlock(blockLocation, blockToUse);
                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, blockToUse));
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
            if (args.Length == 0)
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
                foreach (Vector3 blockLocation in region)
                {
                    // Remove blocks that are not already air. (improves the performance)
                    if (GetBlockFromLocation(blockLocation) != AirID)
                    {
                        PlaceBlock(blockLocation, AirID);
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
                bool useAir = args.Any(a => a.Equals("false", StringComparison.OrdinalIgnoreCase)) == false; // Enabled by default.
                bool pasteAtPoint1 = args.Any(a => a.Equals("pos1", StringComparison.OrdinalIgnoreCase));

                Vector3 basePosition;
                if (pasteAtPoint1)
                    basePosition = _pointToLocation1;
                else
                {
                    // Shift the whole copied box so that: (old player-anchor) -> (new player-anchor).
                    Vector3 playerLoc = GetUsersLocation();
                    basePosition = playerLoc - CopyAnchorOffset;
                }

                // PasteRegion(Region region).
                var region = PasteRegion(basePosition);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(ExtractVector3HashSet(region));
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (var (blockLocation, block) in region)
                {
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
        [Command("/clearc")]
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
            if (args.Length == 0)
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
                        if (args.Length != 2) { Console.WriteLine("ERROR: Missing parameter. Usage: /tool [on/off] [/command]"); return; }

                        _toolCommand = args.Length >= 2 ? string.Join(" ", args.Skip(1)) : string.Empty;
                        _toolItem = GetUsersHeldItem();

                        Timer toolTimer = new Timer() { Interval = 1 };
                        toolTimer.Tick += WorldTool_Tick;
                        toolTimer.Start();

                        _toolEnabled = true;
                        Console.WriteLine($"Tool Activated! Command: {_toolCommand}");
                        break;

                    case "off":
                        _toolEnabled = false;
                        Console.WriteLine("Tool Deactivated!");
                        break;

                    case "command":
                        if (args.Length != 2) { Console.WriteLine("ERROR: Missing parameter. Usage: /tool [on/off] [/command]"); return; }

                        _toolCommand = args.Length >= 2 ? string.Join(" ", args.Skip(1)) : string.Empty;
                        Console.WriteLine($"New Tool Command: {_toolCommand}");
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
        private void ExecuteBrush(string[] args) // Don't give 'static' for brush command.
        {
            if (args.Length == 0)
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
                        {
                            if (args.Length < 1) { Console.WriteLine("ERROR: Missing parameter. Usage: /brush [on/off] (block(,array)) (size)"); return; }

                            string defaultBlock = !string.IsNullOrEmpty(_brushBlockPattern) ? _brushBlockPattern : "1";
                            string blockPattern = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : defaultBlock;

                            // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                            int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                            if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                            _brushItem = GetUsersHeldItem();                                                   // Or use 'WandItemID'.
                            _brushBlockPattern = blockPattern;
                            _brushSize = args.Length > 2 && int.TryParse(args[2], out int s) ? s : _brushSize; // If value is not set, keep set value.

                            // Turn off brushing commands.
                            _brushReplaceMode = false;
                            _brushRapidMode = false;
                            _brushRunTimes = 0;

                            // Make sure we don't stack multiple timers.
                            if (_brushTimer != null)
                            {
                                _brushTimer.Stop();
                                _brushTimer.Tick -= WorldBrush_Tick;
                                _brushTimer.Dispose();
                                _brushTimer = null;
                            }

                            // Create and start a single shared timer.
                            _brushTimer = new Timer { Interval = 1 };
                            _brushTimer.Tick += WorldBrush_Tick;
                            _brushTimer.Start();

                            _brushEnabled = true;
                            break;
                        }

                    case "off":
                        Console.WriteLine("Brush Deactivated!");
                        _brushEnabled = false;

                        if (_brushTimer != null)
                        {
                            _brushTimer.Stop();
                            _brushTimer.Tick -= WorldBrush_Tick;
                            _brushTimer.Dispose();
                            _brushTimer = null;
                        }
                        return;

                    case "block":
                        {
                            if (args.Length != 2)                                              { Console.WriteLine("ERROR: Missing block type. Usage: /brush block [block(,array)]");  return; }

                            string defaultBlock = !string.IsNullOrEmpty(_brushBlockPattern) ? _brushBlockPattern : "1";
                            string blockPattern = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : defaultBlock;

                            // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                            int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                            if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                            _brushBlockPattern = blockPattern;
                            break;
                        }

                    case "shape":
                        if (args.Length != 2 || !IsValidBrushShape(args[1].ToLower()))         { Console.WriteLine("ERROR: Missing shape type. Usage: /brush shape [shape]");          return; }

                        _brushShape = args[1].ToLower();
                        break;

                    case "size":
                        if (args.Length != 2 || !int.TryParse(args[1], out int size))          { Console.WriteLine("ERROR: Invalid brush size. Usage: /brush size [size]");            return; }

                        _brushSize = size;
                        break;

                    case "height":
                        if (args.Length != 2 || !int.TryParse(args[1], out int height))        { Console.WriteLine("ERROR: Invalid brush size. Usage: /brush height [height]");        return; }

                        _brushHeight = height;
                        break;

                    case "hollow":
                        if (args.Length != 2 || !bool.TryParse(args[1], out bool hollow))      { Console.WriteLine("ERROR: Invalid replace mode. Usage: /brush hollow [true/false]");  return; }

                        _brushHollow = hollow;
                        break;

                    case "replace":
                        if (args.Length != 2 || !bool.TryParse(args[1], out bool replaceMode)) { Console.WriteLine("ERROR: Invalid replace mode. Usage: /brush replace [true/false]"); return; }

                        _brushReplaceMode = replaceMode;
                        break;

                    case "rapid":
                        if (args.Length != 2 || !bool.TryParse(args[1], out bool rapidMode))   { Console.WriteLine("ERROR: Invalid rapid mode. Usage: /brush rapid [true/false]");     return; }

                        _brushRapidMode = rapidMode;
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
                                  $"Block Type: {_brushBlockPattern}\n" +
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

        // Utility Commands.

        #region /removenear

        [Command("/removenear")]
        [Command("/nuke")]
        private static void ExecuteRemoveNear(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: Command usage /removenear [radii] (pos1)");
                return;
            }

            try
            {
                int radii = int.TryParse(args[0], out int r) ? r : 1;
                bool pasteAtPoint1 = args.Any(a => a.Equals("pos1", StringComparison.OrdinalIgnoreCase));

                // Define the base location.
                Vector3 basePosition = GetUsersLocation();
                if (pasteAtPoint1)
                    basePosition = _pointToLocation1;

                // Get the shortest distance from the world boundaries.
                int furthestDistance = (int)Math.Max(Math.Abs(basePosition.Y - WorldHeights.MaxY), Math.Abs(basePosition.Y - WorldHeights.MinY));
                int shortestDistance = (int)Math.Min(Math.Abs(basePosition.Y - WorldHeights.MaxY), Math.Abs(basePosition.Y - WorldHeights.MinY));

                // Get the max height based on the input and the cap.
                int searchHeight = (radii <= furthestDistance) ? radii : furthestDistance;
        
                // Get the center point.
                Vector3 centerOffset = new Vector3(basePosition.X, basePosition.Y - (searchHeight / 2), basePosition.Z);

                // MakeCylinder(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, int ignoreBlock = -1).
                var region = MakeCylinder(centerOffset, radii, radii, searchHeight, false, AirID);

                // Save the existing region and clear the upcoming redo.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Place block if it doesn't already exist. (improves the performance).
                    if (GetBlockFromLocation(blockLocation) != AirID)
                    {
                        PlaceBlock(blockLocation, AirID);

                        // Add block to redo.
                        redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, AirID));
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

        #region /replacenear

        [Command("/replacenear")]
        [Command("/renear")]
        private static void ExecuteReplaceNear(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Command usage /replacenear [radii] [source block,(all)] [to block,(all)] (pos1)");
                return;
            }

            try
            {
                int radii = int.TryParse(args[0], out int r) ? r : 1;
                string searchPattern = !string.IsNullOrEmpty(args[1]) ? args[1] : "2";
                string replacePattern = !string.IsNullOrEmpty(args[2]) ? args[2] : "1";
                bool pasteAtPoint1 = args.Any(a => a.Equals("pos1", StringComparison.OrdinalIgnoreCase));

                // Define the base location.
                Vector3 basePosition = GetUsersLocation();
                if (pasteAtPoint1)
                    basePosition = _pointToLocation1;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] searchPatternNumbers = (searchPattern == "all") ? Array.Empty<int>() : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(searchPattern, BlockIDValues);
                if (searchPattern != "all" && searchPatternNumbers.Length == 0) return;   // Make sure the input is within the min/max.
                int[] replacePatternNumbers = (replacePattern == "all") ? Array.Empty<int>() : GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(replacePattern, BlockIDValues);
                if (replacePattern != "all" && replacePatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // Get the shortest distance from the world boundaries.
                int furthestDistance = (int)Math.Max(Math.Abs(basePosition.Y - WorldHeights.MaxY), Math.Abs(basePosition.Y - WorldHeights.MinY));
                int shortestDistance = (int)Math.Min(Math.Abs(basePosition.Y - WorldHeights.MaxY), Math.Abs(basePosition.Y - WorldHeights.MinY));
                
                // Get the max height based on the input and the cap.
                int searchHeight = (radii <= furthestDistance) ? radii : furthestDistance;
        
                // Get the center point.
                Vector3 centerOffset = new Vector3(basePosition.X, basePosition.Y - (searchHeight / 2), basePosition.Z);

                // MakeCylinder(Vector3 pos, double radiusX, double radiusZ, int height, bool hollow, int ignoreBlock = -1).
                // Check if the from-block pattern contains air, and if so, have the region save it.
                var region = (searchPattern == "all" || searchPatternNumbers.Contains(AirID)) ? MakeCylinder(centerOffset, radii, radii, searchHeight, false) : MakeCylinder(centerOffset, radii, radii, searchHeight, false, AirID);

                // Save the existing region and clear the upcoming redo.
                if (searchPattern == "all")
                    SaveUndo(region);
                else
                    SaveUndo(region, saveBlock: searchPatternNumbers);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get the current block type.
                    int currentBlock = GetBlockFromLocation(blockLocation);

                    // Check if the current block is a block to replace.
                    if ((searchPattern == "all" && currentBlock != AirID) || searchPatternNumbers.Contains(currentBlock)) // Make sure not to replace 'air' when using 'all' mode.
                    {
                        // Get random block from input.
                        HashSet<int> excludedBlocks = new HashSet<int> { AirID, 26 }; // IDs to exclude. Block ID 26 'Torch' crashes.
                        int replaceBlock = (replacePattern == "all") ? GetRandomBlock(excludedBlocks) : GetRandomBlockFromPattern(replacePatternNumbers);

                        // Place block if it doesn't already exist. (improves the performance).
                        if (GetBlockFromLocation(blockLocation) != replaceBlock)
                        {
                            PlaceBlock(blockLocation, replaceBlock);

                            // Add block to redo.
                            redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, replaceBlock));
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

        #region /snow

        [Command("/snow")]
        private static void ExecuteSnow(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Command usage /snow [block(,array)] [radius] (pos1)");
                return;
            }

            try
            {
                string blockPattern = !string.IsNullOrEmpty(args[0]) ? args[0] : "1";
                int radius = int.TryParse(args[1], out int r) ? r : 10;
                bool pasteAtPoint1 = args.Any(a => a.Equals("pos1", StringComparison.OrdinalIgnoreCase));

                // Define the base location.
                Vector3 basePosition = GetUsersLocation();
                if (pasteAtPoint1)
                    basePosition = _pointToLocation1;

                // Compare the input string to the games Enums and convert to their numerical values excluding numerical inputs.
                int[] blockPatternNumbers = GetClosestEnumValues<DNA.CastleMinerZ.Terrain.BlockTypeEnum>(blockPattern, BlockIDValues);
                if (blockPatternNumbers.Length == 0) return; // Make sure the input is within the min/max.

                // MakeSnow(Vector3 center, int radius).
                var region = MakeSnow(basePosition, radius);

                // Save the existing region and clear the upcoming redo.
                // Extract and save only the vector locations for the initial save.
                SaveUndo(region);
                ClearRedo();

                HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                foreach (Vector3 blockLocation in region)
                {
                    // Get random block from input.
                    int block = GetRandomBlockFromPattern(blockPatternNumbers);

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

        private Timer _brushTimer;        // The single timer that drives WorldBrush_Tick.
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
                if (sender is Timer t)
                {
                    t.Stop();
                    t.Tick -= WorldBrush_Tick;
                    t.Dispose();

                    if (ReferenceEquals(_brushTimer, t))
                        _brushTimer = null;
                }

                _brushEnabled = false;
                _brushRunTimes = 0;
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
                        SaveUndo(ExtractVector3HashSet(region), saveBlock: new int[] { cursorBlock });
                    else
                        SaveUndo(ExtractVector3HashSet(region));
                    ClearRedo();

                    HashSet<Tuple<Vector3, int>> redoBuilder = new HashSet<Tuple<Vector3, int>>();
                    foreach (var (blockLocation, block) in region)
                    {
                        // Get random block from input.
                        int blockToUse = block;
                        if (block == -1)
                            blockToUse = GetRandomBlockFromPattern(_brushBlockPattern);

                        // Check for replace mode. If so, check if the current block is a block to replace, otherwise continue.
                        if (!_brushReplaceMode || (_brushReplaceMode && GetBlockFromLocation(blockLocation) == cursorBlock) || _brushShape == "snow")
                        {
                            // Place block if it doesn't already exist. (improves the performance)
                            if (GetBlockFromLocation(blockLocation) != blockToUse)
                            {
                                PlaceBlock(blockLocation, blockToUse);

                                // Add block to redo.
                                redoBuilder.Add(new Tuple<Vector3, int>(blockLocation, blockToUse));
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


