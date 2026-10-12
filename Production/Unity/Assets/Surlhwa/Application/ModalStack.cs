using System;
using System.Collections.Generic;
namespace Surlhwa.Application
{
    public sealed class ModalStack
    {
        private readonly Stack<string> stack=new Stack<string>();
        public string Top => stack.Count==0 ? null : stack.Peek(); public int Count => stack.Count;
        public void Push(string id) { if(string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Modal identity required."); if(Top!=id) stack.Push(id); }
        public void Pop() { if(stack.Count>0) stack.Pop(); }
    }
}
