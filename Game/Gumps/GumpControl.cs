﻿#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using ClassicUO.Game.GameObjects.Interfaces;
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using IUpdateable = ClassicUO.Game.GameObjects.Interfaces.IUpdateable;

namespace ClassicUO.Game.Gumps
{
    public class GumpControl : IDrawableUI, IUpdateable
    {
        private readonly List<GumpControl> _children;
        private GumpControl _parent;
        private Rectangle _bounds;
        private Point _lastClickPosition;
        private float _maxTimeForDClick;
        private bool _acceptKeyboardInput, _acceptMouseInput;


        public GumpControl(GumpControl parent = null)
        {
            Parent = parent;
            _children = new List<GumpControl>();
            IsEnabled = true;
            IsVisible = true;
            AllowedToDraw = true;

            AcceptMouseInput = true;
        }


        public event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove, MouseEnter, MouseLeft, MouseClick, MouseDoubleClick;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;
        public event EventHandler<KeyboardEventArgs> Keyboard;


        public bool AllowedToDraw { get; set; }
        public SpriteTexture Texture { get; set; }
        public Vector3 HueVector { get; set; }
        public Serial ServerSerial { get; set; }
        public Serial LocalSerial { get; set; }
        public Point Location
        {
            get => _bounds.Location;
            set { X = value.X; Y = value.Y; }
        }

        public Rectangle Bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        public bool IsDisposed { get; private set; }
        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsFocused { get; protected set; }
        public bool MouseIsOver { get; protected set; }
        public bool CanMove { get; set; }
        public bool CanCloseWithRightClick { get; set; }
        public bool CanCloseWithEsc { get; set; }
        public bool IsEditable { get; set; }
        public IReadOnlyList<GumpControl> Children => _children;

        public virtual bool AcceptKeyboardInput
        {
            get
            {
                if (!IsEnabled || IsDisposed || !IsVisible)
                    return false;

                if (_acceptKeyboardInput)
                    return true;

                foreach (var c in _children)
                    if (c.AcceptKeyboardInput)
                        return true;

                return false;
            }
            set => _acceptKeyboardInput = value;       
        }

        public virtual bool AcceptMouseInput
        {
            get => IsEnabled && !IsDisposed && _acceptMouseInput;
            set => _acceptMouseInput = value;
        }

        public int Width
        {
            get => _bounds.Width;
            set => _bounds.Width = value;
        }

        public int Height
        {
            get => _bounds.Height;
            set => _bounds.Height = value;
        }

        public int X
        {
            get => _bounds.X;
            set => _bounds.X = value;
        }

        public int Y
        {
            get => _bounds.Y;
            set => _bounds.Y = value;
        }

        public int ParentX => Parent != null ? Parent.X + Parent.ParentX : 0;
        public int ParentY => Parent != null ? Parent.Y + Parent.ParentY : 0;

        public GumpControl Parent
        {
            get => _parent;
            set
            {
                if (value == null)
                    _parent?._children.Remove(this);
                else
                    value._children.Add(this);

                _parent = value;

            }
        }

        public GumpControl RootParent
        {
            get
            {
                GumpControl p = this;
                while (p.Parent != null)
                    p = p.Parent;
                return p;
            }
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
            {
                return;
            }

            if (Children.Count > 0)
            {
                int w = 0, h = 0;

                foreach (GumpControl c in Children)
                {
                    c.Update(totalMS, frameMS);

                    if (w < c.Bounds.Right)
                        w = c.Bounds.Right;
                    if (h < c.Bounds.Bottom)
                        h = c.Bounds.Bottom;
                }

                if (w != Width)
                    Width = w;
                if (h != Height)
                    Height = h;
            }
        }

        public virtual bool Draw(SpriteBatchUI spriteBatch,  Vector3 position)
        {
            if (IsDisposed || ((Texture == null || Texture.IsDisposed) && Children.Count <= 0))
            {
                return false;
            }

            if (Texture != null)
                Texture.Ticks = World.Ticks;


            foreach (GumpControl c in Children)
            {
                if (c.IsVisible)
                {
                    Vector3 offset = new Vector3(c.X + position.X, c.Y + position.Y, position.Z);
                    c.Draw(spriteBatch, offset);
                }
            }

            return true;
        }



        internal void SetFocused()
        {
            IsFocused = true;
        }

        internal void RemoveFocus()
        {
            IsFocused = false;
        }



        public GumpControl[] HitTest(Point position)
        {
            List<GumpControl> results = new List<GumpControl>();

            bool inbouds = Bounds.Contains(position.X - ParentX, position.Y - ParentY);

            if (inbouds)
            {
                if (AcceptMouseInput)
                    results.Insert(0, this);

                foreach (var c in Children)
                {
                    var cl = c.HitTest(position);
                    if (cl != null)
                    {
                        for (int i = cl.Length - 1; i >= 0; i--)
                            results.Insert(0, cl[i]);
                    }
                }
            }

            return results.Count == 0 ? null : results.ToArray();
        }

        public GumpControl GetFirstControlAcceptKeyboardInput()
        {
            if (_acceptKeyboardInput)
                return this;
            if (_children == null || _children.Count == 0)
                return null;
            foreach (var c in _children)
            {
                if (c.AcceptKeyboardInput)
                    return c.GetFirstControlAcceptKeyboardInput();
            }
            return null;
        }


        public void AddChildren(GumpControl c)
        {
            c.Parent = this;
        }

        public void RemoveChildren(GumpControl c)
        {
            c.Parent = null;
        }

        public void Clear()
        {
            _children.ForEach(s => s.Parent = null);
            _children.Clear();
        }

        public T[] GetControls<T>() where T : GumpControl => Children.OfType<T>().ToArray();





        public void InvokeMouseDown(Point position, MouseButton button)
        {
            _lastClickPosition = position;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseDown(x, y, button);
            MouseDown.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Pressed));
        }

        public void InvokeMouseUp(Point position, MouseButton button)
        {
            _lastClickPosition = position;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseUp(x, y, button);
            MouseUp.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Released));
        }

        public void InvokeMouseEnter(Point position)
        {
            MouseIsOver = true;
            if (Math.Abs(_lastClickPosition.X - position.X) + Math.Abs(_lastClickPosition.Y - position.Y) > 3)
                _maxTimeForDClick = 0.0f;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseEnter(x, y);
            MouseEnter.Raise(new MouseEventArgs(x, y));
        }

        public void InvokeMouseLeft(Point position)
        {
            MouseIsOver = false;
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            OnMouseLeft(x, y);
            MouseLeft.Raise(new MouseEventArgs(x, y));
        }

        public void InvokeMouseClick(Point position, MouseButton button)
        {
            int x = position.X - X - ParentX;
            int y = position.Y - Y - ParentY;
            float ms = World.Ticks;

            bool doubleClick = false;

            if (_maxTimeForDClick != 0f)
            {
                if (ms <= _maxTimeForDClick)
                {
                    _maxTimeForDClick = 0;
                    doubleClick = true;
                }
            }
            else
                _maxTimeForDClick = ms + 200;

            if (button == MouseButton.Right && RootParent.CanCloseWithRightClick)
            {
                RootParent.Dispose();
            }
            else
            {
                if (doubleClick)
                {
                    OnMouseDoubleClick(x, y, button);
                    MouseDoubleClick.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Pressed));
                }
                else
                {
                    OnMouseClick(x, y, button);
                    MouseClick.Raise(new MouseEventArgs(x, y, button, Microsoft.Xna.Framework.Input.ButtonState.Pressed));
                }
            }
        }

        public void InvokeTextInput(char c)
        {
            OnTextInput(c);
        }

        public void InvokeKeyDown(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {
            OnKeyDown(key, mod);
        }

        public void InvokeKeyUp(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {
            OnKeyUp(key, mod);
        }



        protected virtual void OnMouseDown(int x, int y, MouseButton button)
        {

        }

        protected virtual void OnMouseUp(int x, int y, MouseButton button)
        {

        }

        protected virtual void OnMouseEnter(int x, int y)
        {

        }

        protected virtual void OnMouseLeft(int x, int y)
        {

        }

        protected virtual void OnMouseClick(int x, int y, MouseButton button)
        {

        }

        protected virtual void OnMouseDoubleClick(int x, int y, MouseButton button)
        {

        }

        protected virtual void OnTextInput(char c)
        {
            
        }
     
        protected virtual void OnKeyDown(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {

        }

        protected virtual void OnKeyUp(SDL2.SDL.SDL_Keycode key, SDL2.SDL.SDL_Keymod mod)
        {

        }

        protected virtual bool Contains(int x, int y)
        {
            return true;
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i];
                c.Dispose();
            }

            IsDisposed = true;
        }

    }
}