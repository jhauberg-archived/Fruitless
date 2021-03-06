﻿using ComponentKit;
using ComponentKit.Model;
using OpenTK;
using OpenTK.Input;
using System;
using System.Runtime.InteropServices;

namespace Fruitless {
    public class DebuggableGameWindow : DefaultGameWindow {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        IntPtr _windowHandle;
        IntPtr _consoleHandle;

        bool _isShowingConsole = false;

        IEntityRecord _selectedEntity;

        const string InputSymbol = "<-";
        const string OutputSymbol = "->";

        const string SelectCommand = "select";
        const string SelectCommandShorthand = "sel";

        const string RemoveCommand = "remove";
        const string RemoveCommandShorthand = "rm";

        const string ListCommand = "list";
        const string ListCommandShorthand = "ls";

        const string MakeEntityCommand = "make";
        const string MakeEntityCommandShorthand = "mk";

        const string MakeEntityFromDefinitionCommand = "makedef";
        const string MakeEntityFromDefinitionCommandShorthand = "mkdef";

        public DebuggableGameWindow(int width, int height, string title)
            : base(width, height, title) {
            Console.SetWindowSize(80, 40);
            Console.SetBufferSize(Console.WindowWidth, Int16.MaxValue - 1);

            _windowHandle = FindWindowByCaption(IntPtr.Zero, Title);
            _consoleHandle = FindWindowByCaption(IntPtr.Zero, Console.Title);

            DetermineConsoleTitle();

            Console.WriteLine("Press ~ to toggle console...");
            Console.WriteLine();
        }

        protected override void OnEntityEntered(object sender, EntityEventArgs e) {
            WriteInfo(String.Format("[+] {0}", e.Record));
        }

        protected override void OnEntityRemoved(object sender, EntityEventArgs e) {
            WriteInfo(String.Format("[-] {0}", e.Record));
        }

        protected override void OnUpdateFrame(FrameEventArgs e) {
            base.OnUpdateFrame(e);

            if (KeyWasReleased(Key.Tilde)) {
                ToggleConsole();
            }
        }

        void DetermineConsoleTitle() {
            string title = string.Empty;

            if (_isShowingConsole) {
                title = String.Format("PAUSED: {0}", Title);
            } else {
                title = String.Format("OBSERVING: {0}", Title);
            }

            Console.Title = title;
        }

        protected void ToggleConsole() {
            _isShowingConsole = !_isShowingConsole;

            IntPtr focusedWindowHandle =
                _isShowingConsole ? 
                    _consoleHandle : 
                    _windowHandle;

            if (SetForegroundWindow(focusedWindowHandle)) {
                DetermineConsoleTitle();

                if (_isShowingConsole) {
                    BeginParsingCommand();
                }
            }
        }

        void BeginParsingCommand() {
            ConsoleColor previousForegroundColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(GetTimestampedMessage(
                String.Format("{0} ", InputSymbol)));

            ExecuteCommand(Console.ReadLine());

            Console.ForegroundColor = previousForegroundColor;
        }

        void ExecuteCommand(string command) {
            string[] arguments = command.Split(' ');

            if (arguments.Length > 0) {
                string commandArgument = arguments[0];
                string parameterArgument = string.Empty;

                if (arguments.Length > 1) {
                    parameterArgument = arguments[1];
                }

                bool commandWasExecuted = false;

                switch (commandArgument) {
                    default:
                        break;

                    case ListCommandShorthand:
                    case ListCommand: {
                            commandWasExecuted = List();
                        }
                        break;

                    case SelectCommandShorthand:
                    case SelectCommand: {
                            commandWasExecuted = Select(entityName: parameterArgument);
                        }
                        break;

                    case RemoveCommandShorthand:
                    case RemoveCommand: {
                            commandWasExecuted = Remove(entityName: parameterArgument);
                        }
                        break;

                    case MakeEntityCommandShorthand:
                    case MakeEntityCommand: {
                            commandWasExecuted = Make(string.Empty, parameterArgument);
                        }
                        break;

                    case MakeEntityFromDefinitionCommandShorthand:
                    case MakeEntityFromDefinitionCommand: {
                            string entityName = string.Empty;

                            if (arguments.Length > 2) {
                                entityName = arguments[2];
                            }

                            commandWasExecuted = Make(parameterArgument, entityName);
                        }
                        break;
                }

                if (!commandWasExecuted) {
                    WriteWarning(String.Format("{0} this did nothing",
                        OutputSymbol));
                }
            }

            ToggleConsole();
        }

        bool Select(string entityName) {
            _selectedEntity = Entity.Find(entityName);

            WriteInfo(String.Format("{0} selected: {1}",
                        OutputSymbol,
                        _selectedEntity == null ?
                            "nothing" :
                            _selectedEntity.ToString()));

            if (_selectedEntity != null) {
                return true;
            }

            return false;
        }

        bool Remove(string entityName) {
            if (!string.IsNullOrEmpty(entityName)) {
                if (_selectedEntity != null && _selectedEntity.Name.Equals(entityName)) {
                    _selectedEntity = null;
                }

                if (Entity.Drop(entityName)) {
                    return true;
                } else {
                    WriteWarning(String.Format("{0} that entity was not removed",
                        OutputSymbol));
                }
            } else {
                if (_selectedEntity != null) {
                    return Remove(_selectedEntity.Name);
                }
            }

            return false;
        }

        bool Make(string definition, string named) {
            IEntityRecord entity = null;

            if (string.IsNullOrEmpty(definition)) {
                entity = Entity.Create(named);
            } else {
                if (string.IsNullOrEmpty(named)) {
                    entity = Entity.CreateFromDefinition(definition);
                } else {
                    entity = Entity.CreateFromDefinition(definition, named);
                }
            }

            return entity != null;
        }

        bool List() {
            Console.WriteLine(EntityRegistry.Current.ToString());

            return true;
        }

        string GetTimestampedMessage(string message) {
            return String.Format("{0}: {1}",
                DateTime.Now.ToLongTimeString(),
                message);
        }

        protected void WriteWarning(string message) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(GetTimestampedMessage(message));
        }

        protected void WriteInfo(string message) {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(GetTimestampedMessage(message));
        }
    }
}
