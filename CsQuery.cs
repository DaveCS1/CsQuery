﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.Web.Script.Serialization;
using Jtc.CsQuery.ExtensionMethods;
using Jtc.CsQuery.Utility;

namespace Jtc.CsQuery
{
    /// <summary>
    /// Relationship between Dom and Selections
    /// 
    /// Dom represents a "DOM" and is shared among related CsQuery objects. This is the actual heirarchy. When a subquery is run, it 
    /// uses the same DOM as its parent. 
    /// Selections is a set of elements matching a selector. It is always a subset of DOM. 
    /// 
    /// DomRoot is a reference to the original CsQuery object that a DOM was created. 
    /// 
    /// Only CsQuery.Create creates a new DOM. Any other methods create unbound elements.
    /// Cloning elements also creates unbound elements. They are not part of any DOM until added. So removing something from a clone will have no effect on 
    /// a DOM.
    /// </summary>
    
    public class CsQuery : IEnumerable<IDomObject>
    {
        // Used to manage extension methods by keeping a reference within the base CsQuery to whatever object it creates.
        internal Dictionary<string, object> ExtensionCache
        {
            get
            {
                if (DomOwner != null)
                {
                    return DomOwner.ExtensionCache;
                } else {
                    if (_ExtensionCache == null)
                    {
                        _ExtensionCache = new Dictionary<string, object>();
                    }
                }
                return _ExtensionCache;
            }
        } protected Dictionary<string, object> _ExtensionCache = null;

        /// <summary>
        /// The CsQuery object that contains the actual DOM for which is is a selector. 
        /// </summary>
        protected CsQuery DomOwner { get; set; }

        protected DomElementFactory ElementFactory
        {
            get
            {
                if (_ElementFactory == null)
                {
                    _ElementFactory = new DomElementFactory();
                }
                return (_ElementFactory);
            }
        } private DomElementFactory _ElementFactory = null;

        /// <summary>
        /// Creates a new DOM. This will DESTROY any existing DOM. This is not the same as Select.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public void CreateNew(string html)
        {
            _Dom = new DomRoot();
            _Dom.Owner = this;
            ClearSelections();
            Dom.ChildNodes.AddRange(ElementFactory.CreateObjects(html));
            AddSelectionRange(Dom.ChildNodes);
        }
        /// <summary>
        /// Creates a new DOM. This will DESTROY any existing DOM. This is not the same as Select.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public void CreateNew(IEnumerable<IDomObject> elements)
        {
            _Dom = new DomRoot();
            _Dom.Owner = this;
            ClearSelections();
            Dom.ChildNodes.AddRange(elements);
            AddSelectionRange(Dom.ChildNodes);
        }
        
        //public static CsQuery CreateFromElements(IEnumerable<IDomObject> elements)
        //{
        //    CsQuery csq = new CsQuery();
        //    csq.Dom.AddRange(elements);
        //    csq.AddSelectionRange(elements);
        //    return csq;
        //}
        /// <summary>
        /// Creates a new, empty jQuery object.
        /// </summary>
        public CsQuery()
        {
            
        }
        public CsQuery(string selector)
        {
            CreateNew(selector);
        }
        public CsQuery(string selector, string css)
        {
            CreateNew(selector);
            AttrSet(css);

        }
        /// <summary>
        /// Create a new CsQuery object using an existing instance and a selector
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        public CsQuery(string selector, CsQuery context)
        {
            // when creating a new CsQuery from another, leave Dom blank - it will be populated automatically with the
            // contents of the selector.
            DomOwner = context;

            //Find(selector);

            if (selector == null)
            {
                AddSelectionRange(context.Children());
            }
            else
            {
                Selectors = new CsQuerySelectors(selector);
                AddSelectionRange(Selectors.Select(Dom,context.Children()));
            }
        }
        /// <summary>
        /// Create a new CsQuery from a single elemement, using its own context
        /// </summary>
        /// <param name="element"></param>
        public CsQuery(IDomObject element)
        {
            DomOwner = element.Owner;
            AddSelection(element);
        }
        /// <summary>
        /// Create a new CsQuery object from a set of DOM elements, using the DOM of the first element.
        /// could contain more than one context)
        /// </summary>
        /// <param name="elements"></param>
        public CsQuery(IEnumerable<IDomObject> elements)
        {
            List<IDomObject> elList = new List<IDomObject>(elements);

            if (elList.Count==0) {
                return;
            }
            DomOwner = elList[0].Owner;
            AddSelectionRange(elList);
        }

        /// <summary>
        /// Create a new CsQuery object from a set of DOM elements, using the DOM of the first element.
        /// could contain more than one context)
        /// </summary>
        /// <param name="elements"></param>
        public CsQuery(IEnumerable<IDomObject> elements, CsQuery context)
        {
            DomOwner = context;
            AddSelectionRange(elements);
        }

        /// <summary>
        /// Create a new CsQuery from a single DOM element
        /// </summary>
        /// <param name="element"></param>
        public CsQuery(IDomObject element, CsQuery context)
        {
            DomOwner = context;
            AddSelection(element);
        }

        /// <summary>
        /// Creeate a new DOM from HTML text
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static CsQuery Create(string html)
        {
            return new CsQuery(html);
        }
        public static CsQuery Create(string html, string attributes)
        {
            return new CsQuery(html, attributes);
        }
        /// <summary>
        /// Creates a new DOM from a file
        /// </summary>
        /// <param name="htmlFile"></param>
        /// <returns></returns>
        public static CsQuery LoadFile(string htmlFile)
        {
            return new CsQuery(Support.GetFile(htmlFile));
        }
        /// <summary>
        /// Creeate a new DOM from elements
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static CsQuery Create(IEnumerable<IDomObject> elements)
        {
            CsQuery csq = new CsQuery();
            csq.CreateNew(elements);
            return csq;
        }
        /// <summary>
        /// Mimics .Create
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static implicit operator CsQuery(string html)
        {
            return CsQuery.Create(html);
        }
        /// <summary>
        /// Create a new CsQuery object using the DOM context from an element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static implicit operator CsQuery(DomElement element)
        {
            CsQuery csq = new CsQuery(element);
            return csq;
        }

        /// <summary>
        /// Represents the full, parsed DOM for an object created with an HTML parameter
        /// </summary>
        public DomRoot Dom 
        {
            get {
                if (_Dom == null)
                {
                    if (DomOwner != null)
                    {
                        _Dom = DomOwner.Dom;
                    }
                    else
                    {
                        _Dom = new DomRoot();
                        _Dom.Owner =this;
                    }
                }
                return _Dom;
            }
            //set
            //{
            //    _Dom = value;
            //    ClearSelections();
            //    if (_Dom != null)
            //    {
            //        AddSelectionRange(_Dom.Children);
            //    }
            //}
        } protected DomRoot _Dom = null;
        /// <summary>
        /// Only elements that match the selection. 
        /// </summary>
        protected IEnumerable<IDomObject> Selection
        {
            get
            {
                if (_Selection == null)
                {
                    _Selection = new List<IDomObject>();
                }
                return _Selection;
            }
        }
        protected List<IDomObject> _Selection = new List<IDomObject>();
        private HashSet<IDomObject> _SelectionUnique = new HashSet<IDomObject>();
        
        /// <summary>
        /// Add an item to the list of selected elements. It should be part of this DOM.
        /// </summary>
        /// <param name="element"></param>
        protected void AddSelection(IDomObject element) {
            //if (!ReferenceEquals(element.Dom, Dom))
            //{
            //    throw new Exception("Cannot add unbound elements or elements bound to another DOM directly to a selection set.");
            //}
            if (_SelectionUnique.Add(element))
            {
                _Selection.Add(element);
            }
        }
        /// <summary>
        /// Returns true if any elements were added
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        protected bool AddSelectionRange(IEnumerable<IDomObject> elements)
        {
            bool result = false;
            foreach (IDomObject elm in elements)
            {
                result = true;
                AddSelection(elm);
            }
            return result;
        }
        protected void ClearSelections()
        {
            _Selection.Clear();
            _SelectionUnique.Clear();
        }
        /// <summary>
        /// Returns just IDomElements from the selection list.
        /// </summary>
        public IEnumerable<IDomElement> Elements
        {
            get
            {
                foreach (IDomObject obj in Selection)
                {
                    if (obj is DomElement)
                    {
                        yield return (DomElement)obj;
                    }
                }
            }
        }

        public int Length
        {
            get
            {
                return _Selection.Count;
            }
        }
        /// <summary>
        /// Return matched element. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IDomObject this[int index]
        {
            get {
                return Get(index);
            }
        }

        
        public CsQuery this[string selector]
        {
            get
            {
                return Select(selector);
            }
        }

        public CsQuery this[IDomObject element]
        {
            get
            {
                return Select(element);
            }
        }
        public CsQuery this[IEnumerable<IDomObject> element]
        {
            get
            {
                return Select(element);
            }
        }
        public IEnumerable<IDomObject> Get()
        {
            return Selection;
        }
        public IDomObject Get(int index)
        {
            int effectiveIndex = index < 0 ? _Selection.Count+index-1 : index;

            if (effectiveIndex >= 0 && effectiveIndex < _Selection.Count)
            {
                return (IDomObject)_Selection[effectiveIndex];
            }
            else
            {
                return null;
            }

        }
        /// <summary>
        ///  The selector (parsed) used to create this instance
        /// </summary>
        public CsQuerySelectors Selectors
        {
            get
            {
                return _Selectors;
            }
            protected set
            {
                _Selectors = value;
            }
        } protected CsQuerySelectors _Selectors = null;



        /// <summary>
        /// Set the HTML contents of each element in the set of matched elements. Any elements without InnerHtml are ignored.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public CsQuery Html(string html)
        {
            DomRoot newElements = new DomRoot(ElementFactory.CreateObjects(html));
            foreach (DomElement obj in Selection)
            {
                if (obj.InnerHtmlAllowed)
                {
                    obj.ChildNodes.Clear();
                    obj.ChildNodes.AddRange(newElements.ChildNodes);
                }
            }
            return this;
        }
        /// <summary>
        /// Get the HTML contents of the first element in the set of matched elements.
        /// </summary>
        /// <returns></returns>
        public string Html()
        {
            if (Length > 0)
            {
                return this[0].InnerHtml;
            }
            else
            {
                return String.Empty;
            }
        }
        /// <summary>
        /// Set the content of each element in the set of matched elements to the specified text.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery  Text(string value)
        {
            foreach (IDomElement obj in Elements)
            {
                if (obj.InnerHtmlAllowed)
                {
                    obj.ChildNodes.Clear();
                    DomText text = new DomText(value);
                    obj.ChildNodes.Add(text);
                }
            }
            return this;
        }
        /// <summary>
        /// Get the combined text contents of each element in the set of matched elements, including their descendants.
        /// </summary>
        /// <returns></returns>
        public string Text()
        {
            StringBuilder sb = new StringBuilder();
            Text(sb,Contents());
            return sb.ToString();
        }
        /// <summary>
        /// Helper for public Text() function to act recursively
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="elements"></param>
        protected void Text(StringBuilder sb, IEnumerable<IDomObject> elements)
        {
            foreach (IDomObject obj in elements)
            {
                switch (obj.NodeType)
                {
                    case NodeType.TEXT_NODE:
                    case NodeType.CDATA_SECTION_NODE:
                    case NodeType.COMMENT_NODE:
                        sb.Append((sb.Length==0 ? String.Empty: " ") + obj.NodeValue);
                        break;
                    case NodeType.ELEMENT_NODE:
                        Text(sb, obj.ChildNodes);
                        break;
                }
            }
        }
        /// <summary>
        /// Returns the HTML for all selected documents, separated by commas. No inner html or children are included.
        /// </summary>
        /// 
        public string SelectionHtml()
        {
            return SelectionHtml(false);
        }
        /// <summary>
        /// Renders just the selection set completely.
        /// </summary>
        /// <returns></returns>
        public string RenderSelection()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IDomObject elm in this)
            {
                sb.Append(elm.Render());
            }
            return sb.ToString();
        }
        public string SelectionHtml(bool includeInner)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IDomObject elm in this)
            {
                
                sb.Append(sb.Length == 0 ? String.Empty : ", ");
                sb.Append(includeInner ? elm.Render() : elm.ToString());
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return SelectionHtml();
        }
         /// <summary>
        /// Renders the DOM to a string
        /// </summary>
        /// <returns></returns>
        public string Render()
        {
            return Dom.Render();
        }
        public string Render(DomRenderingOptions renderingOptions)
        {
            Dom.DomRenderingOptions = renderingOptions;
            return Render();
        }
        /// <summary>
        /// Add elements
        /// </summary>
        /// <param name="?"></param>
        /// <param name="selector"></param>
        protected IEnumerable<IDomObject> _FilterElements(IEnumerable<IDomObject> elements, string selector)
        {
            if (selector.IsNullOrEmpty())
            {
                return Objects.EmptyEnumerable<IDomObject>();
            }
            else
            {
                CsQuerySelectors selectors = new CsQuerySelectors(selector);
                if (selectors.Count > 0)
                {
                    selectors[0].TraversalType = TraversalType.Filter;
                }
                return selectors.Select(Dom, elements);
                
            }
        }
        #region jQuery Methods
        /// <summary>
        ///  Add elements to the set of matched elements. Returns a NEW jQuery object
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public CsQuery Add(IEnumerable<IDomObject> elements)
        {
            CsQuery res = new CsQuery(this);
            res.AddSelectionRange(elements);
            return res;
        }

        /// <summary>
        /// Add elements to the set of matched elements from an HTML fragment. Returns a new jQuery object.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public CsQuery Add(string html)
        {
            return Add(ElementFactory.CreateElements(html));
        }

        /// <summary>
        /// Adds the specified class(es) to each of the set of matched elements.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public CsQuery AddClass(string className)
        {
            foreach (var item in Elements)
            {
                item.AddClass(className);
            }
            return this;
        }
        /// <summary>
        /// Insert content, specified by the parameter, to the end of each element in the set of matched elements.
        /// TODO: Add overloads with multiple values
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public CsQuery Append(string content)
        {
            IEnumerable<IDomObject> els = ElementFactory.CreateObjects(content);
            return Append(els);
        }
        public CsQuery Append(IDomObject element)
        {
            bool first = true;
            foreach (var obj in Elements)
            {
                if (element is IDomContainer)
                {
                    obj.AppendChild(first ? element : element.Clone());
                    first = false;
                }
            }
            return this;
        }
        public CsQuery Append(IEnumerable<IDomObject> elements)
        {
            bool first = true;
            foreach (var obj in Elements )
            {
                foreach (var e in elements)
                {
                    obj.AppendChild(first ? e : e.Clone());
                }
                first = false;
            }
            return this;
        }
        /// <summary>
        ///  Insert every element in the set of matched elements to the end of the target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CsQuery AppendTo(string target)
        {
            return AppendTo(Select(target));

        }
        public CsQuery AppendTo(IEnumerable<IDomObject> target)
        {
            foreach (IDomObject e in target)
            {
                if (e is IDomContainer) {
                    foreach (IDomObject obj in Selection)
                    {
                        e.AppendChild(obj);
                    }

                }
            }
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="func">
        /// delegate(int index, string html) A function that returns an HTML string to insert at the end of each element in the set of matched elements. 
        /// Receives the index position of the element in the set and the old HTML value of the element as arguments.
        /// </param>
        /// <returns></returns>
        public CsQuery Append(Func<int, string, string> func)
        {
            int index = 0;
            foreach (DomElement obj in Elements)
            {
                string clientValue = func(index, obj.InnerHtml);
                IEnumerable<IDomObject> objects = ElementFactory.CreateObjects(clientValue);
                obj.ChildNodes.AddRange(objects); ;
                index++;
            }
            return this;
        }
        /// <summary>
        /// Get the value of an attribute for the first element in the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string Attr(string name)
        {
            if (Length > 0)
            {
                string value;
                if (this[0].TryGetAttribute(name, out value))
                {
                    if (HtmlDom.BooleanAttributes.Contains(name))
                    {
                        // Pre-1.6 and 1.6.1+ compatibility: always return the name of the attribute if it exists for
                        // boolean attributes
                        return name;
                    }
                    else
                    {
                       
                        return value;
                    }
                } else if (this[0].NodeName=="textarea") {
                    return this[0].InnerText;
                }
            }
            return null;
        }

        public CsQuery AttrSet(object attributes) 
        {
            
            if (attributes is string)
            {
                if (((string)attributes).IsJson())
                {
                    attributes = CsQuery.ParseJSON((string)attributes);
                }
                else
                {
                    throw new Exception("AttrSet(object) must be passed a JSON string, object, or ExpandoObject");
                }
            }
            
            IDictionary<string, object> data = (IDictionary<string, object>)attributes.ToExpando();

            foreach (IDomElement el in Elements)
            {
                foreach (var kvp in data)
                {
                    string name = kvp.Key.ToLower();
                    switch(name) {
                        case "css":
                            Select(el).CssSet((IDictionary<string,object>)kvp.Value);
                            break;
                        case "html":
                            Select(el).Html(kvp.Value.ToString());
                            break;
                        case "height":
                        case "width":
                            // for height and width, do not set attributes - set css
                            Select(el).Css(name, kvp.Value.ToString());
                            break;
                        case "text":
                            Select(el).Text(kvp.Value.ToString());
                            break;
                        default:
                            el.SetAttribute(kvp.Key, kvp.Value.ToString());
                            break;
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// Set one or more attributes for the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Attr(string name, object value)
        {
            bool isBoolean = HtmlDom.BooleanAttributes.Contains(name.ToLower());
            if (isBoolean)
            {
                // Using attr with empty string should set a property to "true. But prop() itself requires a truthy value. Check for this specifically.
                if (value is string && (string)value == String.Empty)
                {
                    value = true;
                }
                SetProp(name, value);
                return this;
            }

            string val;
            if (value is bool)
            {
                val = value.ToString().ToLower();
            }
            else
            {
                val = value.ToString();
            }

            foreach (DomElement e in Elements)
            {
                if ((e.NodeName =="input" || e.NodeName=="button") && name == "type" && e.Owner!=null)
                {
                    throw new Exception("Can't change type of input elements in DOM");
                }
                e.SetAttribute(name, val);
            }
            return this;
        }
        public CsQuery RemoveAttr(string name)
        {
            foreach (DomElement e in Elements)
            {
                e.RemoveAttribute(name);
            }
            return this;
        }
        /// <summary>
        /// Insert content, specified by the parameter, before each element in the set of matched elements.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public CsQuery Before(string selector)
        {
            CsQuery csq = new CsQuery(selector, this);
            return Before(csq);

        }
        public CsQuery Before(CsQuery selection)
        {
            foreach (IDomObject element in this)
            {
                int index = GetElementIndex(element);
                foreach (IDomObject obj in selection)
                {
                    element.ParentNode.ChildNodes.Insert(index,obj);
                    index++;
                }
            }
            return this;
        }
        /// <summary>
        /// Get the children of each element in the set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <returns></returns>
        public CsQuery Children()
        {
            return Children(null);
        }
        public CsQuery Children(string selector)
        {
            if (!String.IsNullOrEmpty(selector))
            {
                return new CsQuery(_FilterElements(SelectionChildren(), selector), this);
            }
            else
            {
                return new CsQuery(SelectionChildren(), this);
            }
        }
        /// <summary>
        /// Return all children of all selected elements
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<IDomObject> SelectionChildren()
        {
            foreach (IDomObject obj in Elements)
            {
                foreach (IDomObject child in obj.Elements)
                {
                    yield return child;
                }
            }
        }
        /// <summary>
        /// Create a deep copy of the set of matched elements.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public CsQuery Clone()
        {
            CsQuery csq = new CsQuery();
            
            foreach (IDomObject elm in Elements)
            {
                IDomObject clone = elm.Clone();
                csq.Dom.AppendChild(clone);
                csq.AddSelection(clone);
                
            }
            return csq;
        }
        /// <summary>
        /// Get the first ancestor element that matches the selector, beginning at the current element and progressing up through the DOM tree.
        /// </summary>
        /// <returns></returns>
        public CsQuery Closest(string selector)
        {
            CsQuery matchTo = Select(selector);
            return Closest(matchTo);
        }
        public CsQuery Closest(IDomObject element)
        {
            return Closest(Objects.Enumerate(element));
        }
        public CsQuery Closest(IEnumerable<IDomObject> elements)
        {
            HashSet<IDomObject> selectionSet;
            if (elements is CsQuery)
            {
                selectionSet = ((CsQuery)elements)._SelectionUnique;
            }
            else
            {
                selectionSet = new HashSet<IDomObject>();
                selectionSet.AddRange(elements);
            }
            CsQuery csq = new CsQuery();
            csq.DomOwner = this;

            foreach (var el in Selection)
            {
                var search = el;
                while (search != null)
                {
                    if (selectionSet.Contains(search))
                    {
                        csq.AddSelection(search);
                        return csq;
                    }
                    search = search.ParentNode;
                }

            }
            return csq;

        }
        /// <summary>
        /// Get the children of each element in the set of matched elements, including text and comment nodes.
        /// </summary>
        /// <returns></returns>
        public CsQuery Contents()
        {

            List<IDomObject> list = new List<IDomObject>();
            foreach (IDomObject obj in Selection)
            {
                if (obj is IDomContainer)
                {
                    list.AddRange(obj.ChildNodes );
                }
            }

            return new CsQuery(list, this);
        }
        /// <summary>
        ///  Set one or more CSS properties for the set of matched elements.
        /// </summary>
        /// <param name="cssJson"></param>
        /// <returns></returns>
        public CsQuery CssSet(string cssJson)
        {
            IDictionary<string, object> dict = (ExpandoObject)ParseJSON(cssJson);
            return CssSet(dict);
        }
        public CsQuery CssSet(object css)
        {
            IDictionary<string, object> data = css.ToExpando();

            return this.Each((IDomElement e) =>
            {
                foreach (var key in data)
                {
                    e.Style[key.Key]= key.Value.ToString();
                }
            });
        }
        /// <summary>
        ///  Set one or more CSS properties for the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Css(string name, string value)
        {
            string style = String.Empty;

            foreach (IDomElement e in Elements)
            {
                e.Style[name]=value;
            }
            return this;
        }

        /// <summary>
        /// Get the value of a style property for the first element in the set of matched elements
        ///  NOTE: This is the equivalent of Css( name ) in the original API. Because we cannot overlaod with signatures
        ///  only differing by return type, the method name must be changed.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public string Css(string style)
        {
            if (Length == 0)
            {
                return null;
            }
            else
            {
                return ((IDomElement)this[0]).Style[style];
            }
        }
        /// <summary>
        /// Returns value at named data store for the first element in the jQuery collection, as set by data(name, value).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public object getData<T>(string key) {
            string json = getData(key);
            return json.FromJSON<T>();
        }
        public string getData(string key) {
            return this.First()[0].GetAttribute("data-"+key);
        }
        /// <summary>
        /// Store arbitrary data associated with the specified element. Returns the value that was set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="jsonData"></param>
        /// <returns></returns>
        public CsQuery Data(string key,string jsonData)
        {
            this.Each((IDomElement e) =>
            {
                e.SetAttribute("data-" + key, jsonData);
            });
            return this;
        }
        public CsQuery Data(string key, object data)
        {
            this.Each((IDomElement e) =>
            {
                e.SetAttribute("data-" + key, CsQuery.ToJSON(data));
            });
            return this;
        }
        /// <summary>
        /// Returns value at named data store for the first element in the jQuery collection, as set by data(name, value).
        /// </summary>
        public object Data(string element)
        {
            return CsQuery.ParseJSON(First().Attr("data-" + element));
        }
        /// <summary>
        /// Iterate over each matched element.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public CsQuery Each(Action<int, IDomElement> func)
        {
            int index = 0;
            foreach (IDomElement obj in Elements)
            {
                func(index, obj);
            }
            return this;
        }

        /// <summary>
        /// Iterate over each matched element.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>

        public CsQuery Each(Action<IDomElement> func)
        {
            foreach (IDomElement obj in Elements)
            {
                func(obj);
            }
            return this;
        }
        /// <summary>
        /// Reduce the set of matched elements to the one at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public CsQuery Eq(int index)
        {
            if (index < 0)
            {
                index = Length + index-1;
            }
            if (index >= 0 && index < Length)
            {
                return new CsQuery(this[index], this);
            }
            else
            {
                return EmptySelection();
            }
        }
        protected CsQuery EmptySelection()
        {
            CsQuery empty = new CsQuery();
            empty.DomOwner = this;
            return empty;
        }
        /// <summary>
        /// Get the descendants of each element in the current set of matched elements, filtered by a selector, jQuery object, or element.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Find(string selector)
        {
            CsQuery csq = new CsQuery();
            csq.DomOwner = this;
            CsQuerySelectors selectors = new CsQuerySelectors(selector);
            csq.AddSelectionRange(selectors.Select(Dom,Children()));
            return csq;
        }
        public CsQuery Find(IEnumerable<IDomObject> elements)
        {
            CsQuery csq = new CsQuery();
            csq.DomOwner = this;
            CsQuerySelectors selectors = new CsQuerySelectors(elements);
            csq.AddSelectionRange(selectors.Select(Dom, Children()));
            return csq;
        }
        public CsQuery Find(IDomObject element)
        {
            CsQuery csq = new CsQuery();
            csq.DomOwner = this;

            CsQuerySelectors selectors = new CsQuerySelectors(element);
            csq.AddSelectionRange(selectors.Select(Dom, Children()));
            return csq;
        }

        public CsQuery Filter(string selector)
        {
            return new CsQuery(_FilterElements(Selection, selector));

        }

        /// <summary>
        /// Select from the whole DOM and return a new CSQuery object 
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Select(string selector)
       {
           CsQuerySelectors selectors = new CsQuerySelectors(selector);
           CsQuery csq;

           // Unfortunately, we do not want to create a new CsQuery object when selecting from HTML string.
           // This would disconnect it from the DOM making chained methdods not work. Use CsQuery.Create if this is desired.

           //if (selectors.Count > 0 && selectors[0].SelectorType.HasFlag(SelectorType.HTML))
           //{
           //    csq = Create(selector);
           // }
           //else
           //{
               csq = new CsQuery();
               csq.DomOwner = this;
               csq.AddSelectionRange(selectors.Select(Dom));
           //}
            
           return csq;
        }

        public CsQuery Select(IDomObject element)
        {
            CsQuery csq = new CsQuery(element);
            return csq;
        }
        public CsQuery Select(IEnumerable<IDomObject> elements)
        {
            CsQuery csq = new CsQuery(elements);
            return csq;
        }


        /// <summary>
        /// Reduce the set of matched elements to the first in the set.
        /// </summary>
        /// <returns></returns>
        public CsQuery First()
        {
            return Eq(0);
        }
        /// <summary>
        /// Reduce the set of matched elements to the last in the set.
        /// </summary>
        /// <returns></returns>
        public CsQuery Last()
        {
            return Eq(_Selection.Count - 1);
        }
        /// <summary>
        /// Hide the matched elements.
        /// </summary>
        /// <returns></returns>
        public CsQuery Hide()
        {
            return this.Each((IDomElement e) =>
            {
                e.Style["display"]= "none";
            });
        }

        public int Index()
        {
            IDomObject el = Selection.FirstOrDefault();
            if (el != null)
            {
                return GetElementIndex(el);
            }
            return -1;
        }
        //public int Index(IDomObject element)
        //{
        //     return GetElementIndex(element);
        //}
        /// <summary>
        /// Insert every element in the set of matched elements after the target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CsQuery InsertAfter(IDomObject target)
        {
            return InsertAtOffset(target,1);
        }
        public CsQuery InsertAfter(CsQuery target) {
            return InsertAtOffset(target, 1);
        }
        /// <summary>
        /// Support for InsertAfter and InsertBefore. An offset of 0 will insert before the current element. 1 after.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        protected CsQuery InsertAtOffset(IDomObject target, int offset)
        {
            int index = GetElementIndex(target);
            foreach (var item in this)
            {
                target.ParentNode.ChildNodes.Insert(index+offset,item);
                index++;
            }
            return this;
        }
        public CsQuery InsertAtOffset(CsQuery target, int offset)
        {
            bool isFirst = true;
            foreach (IDomObject e in target)
            {
                if (e is IDomElement)
                {
                    if (isFirst)
                    {
                        InsertAtOffset(e, offset);
                        isFirst = false;
                    }
                    else
                    {
                        e.Clone();
                        IDomObject clone = e.Clone();
                        InsertAtOffset(clone, offset);
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// A selector, element, HTML string, or jQuery object; the matched set of elements will be inserted before the element(s) specified by this parameter.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CsQuery InsertBefore(IDomObject target)
        {
            return InsertAtOffset(target, 0);
        }
        public CsQuery InsertBefore(CsQuery target)
        {
            return InsertAtOffset(target, 0);
        }

        /// <summary>
        /// Get the immediately following sibling of each element in the set of matched elements. 
        /// If a selector is provided, it retrieves the next sibling only if it matches that selector.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Next(string selector)
        {
            if (String.IsNullOrEmpty(selector))
            {
                return new CsQuery(AdjacentElements(true), this);
            }
            else
            {
                return new CsQuery(_FilterElements(AdjacentElements(true), selector), this);
            }
        }
        /// <summary>
        /// if true, then next elements are returned, otherwise, previous
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        protected IEnumerable<IDomObject> AdjacentElements(bool getNext)
        {
            foreach (IDomElement obj in Elements)
            {
                // Extraordinarily inefficient way to get next. TODO: make structure a linked list

                IDomElement last = null;
                var children = obj.ParentNode.Elements.GetEnumerator();
                //children.Reset();
                bool found = false;
                while (children.MoveNext())
                {
                    
                    if (found && children.Current.NodeType == NodeType.ELEMENT_NODE)
                    {
                        yield return (IDomElement)children.Current;
                        break;
                    }
                    if (ReferenceEquals(children.Current, obj))
                    {
                        if (!getNext)
                        {
                            yield return last;
                            break;
                        }
                        else
                        {
                            found = true;
                        }
                    }
                    last = (IDomElement)children.Current;
                }

            }
            yield break;
        }
        public CsQuery Next()
        {
            return Next(null);
        }

        /// <summary>
        /// Get the parent of each element in the current set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Parent()
        {
            return Parent(null);
        }
        public CsQuery Parent(string selector)
        {
            List<IDomElement> list = new List<IDomElement>();

            foreach (IDomElement obj in Elements)
            {
                if (obj.ParentNode is IDomElement)
                {
                    list.Add((IDomElement)obj.ParentNode);
                }
            }
            return new CsQuery(_FilterElements(list, selector), this);
        }
        /// <summary>
        /// Get the immediately preceding sibling of each element in the set of matched elements, optionally filtered by a selector.
        /// </summary>
        /// <returns></returns>
        public CsQuery Prev()
        {
            return Prev(null);
        }
        /// <summary>
        /// Set one or more properties for the set of matched elements.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public CsQuery Prop(string name, object value)
        {
            // Prop actually works on things other than boolean - e.g. SelectedIndex. For now though only use prop for booleans

            if (HtmlDom.BooleanAttributes.Contains(name.ToLower())) {
                SetProp(name, value);
            }
            return this;
        }
        public bool Prop(string name)
        {
            if (Length>0 && HtmlDom.BooleanAttributes.Contains(name.ToLower())) {
                return this[0].HasAttribute(name);
            }
            return false;
        }
        /// <summary>
        /// Helper function for Attr & Prop
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected void SetProp(string name, object value)
        {
            bool state = value.IsTruthy();
            foreach (IDomElement e in Elements)
            {
                if (state)
                {
                    e.SetAttribute(name);
                }
                else
                {
                    e.RemoveAttribute(name);
                }
            }
        }

        public CsQuery Prev(string selector)
        {
            if (String.IsNullOrEmpty(selector))
            {
                return new CsQuery(AdjacentElements(false), this);
            }
            else
            {
                return new CsQuery(_FilterElements(AdjacentElements(false), selector), this);
            }
        }
   

        /// <summary>
        /// Remove all selected elements from the DOM
        /// </summary>
        /// <returns></returns>
        public CsQuery Remove()
        {
            for (int i=_Selection.Count-1;i>=0;i--)
            {
                IDomObject e=_Selection[i];
                e.Remove();
            }
            return this;
        }
        /// <summary>
        /// Remove all elements matching the selector from the DOM
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery Remove(string selector)
        {
            foreach (IDomElement e in Filter(selector))
            {
                e.Remove();
            }
            return this;
        }
        
        /// <summary>
        /// Remove a single class, multiple classes, or all classes from each element in the set of matched elements.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public CsQuery RemoveClass(string className)
        {
            foreach (var elm in this.Elements)
            {
                elm.RemoveClass(className);
            }
            return this;
        }
        /// <summary>
        /// Remove a previously-stored piece of data.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public CsQuery RemoveData()
        {
            foreach (IDomElement el in Elements)
            {
                List<string> toRemove = new List<string>();
                foreach (var kvp in el.Attributes)
                {
                    if (kvp.Key.StartsWith("data"))
                    {
                        toRemove.Add(kvp.Key);
                    }
                    foreach (string key in toRemove)
                    {
                        el.Attributes.Remove(key);
                    }
                }
            }
            return this;
        }
        public CsQuery RemoveData(string element)
        {
            foreach (IDomElement el in Elements)
            {
               List<string> toRemove = new List<string>();
               foreach (var kvp in el.Attributes)
               {
                    if (kvp.Key=="data-"+element)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
               foreach (string key in toRemove)
               {
                   el.Attributes.Remove(key);
               }
            }
            return this;
        }
        /// <summary>
        /// Replace each element in the set of matched elements with the provided new content.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public CsQuery ReplaceWith(string selector)
        {
            CsQuery newContent = new CsQuery(selector, this);
            return Before(selector).Remove();
        }
        public CsQuery ReplaceWith(CsQuery selection)
        {
            return Before(selection).Remove();
        }
        public CsQuery Show()
        {
            foreach (IDomElement e in Elements)
            {
                e.RemoveStyle("display");
            }
            return this;
        }
        /// <summary>
        /// Get the current value of the first element in the set of matched elements.
        /// </summary>
        /// <returns></returns>
        public string Val()
        {
            if (Length > 0)
            {
                IDomElement e = this.Elements.First();
                switch(e.NodeName) {
                    case "textarea":
                        return e.InnerText;
                    case "input":
                        string val = e.GetAttribute("value",String.Empty);
                        switch(e.GetAttribute("type",String.Empty)) {
                            case "radio":
                            case "checkbox":
                                if (String.IsNullOrEmpty(val))
                                {
                                    val = "on";
                                }
                                break;
                            default:
                                break;
                        }
                        return val;
                    case "select":
                        string result = String.Empty;
                        // TODO optgroup handling (just like the setter code)
                        foreach (IDomElement child in e.Elements)
                        {
                            if (e.NodeName == "option" && e.HasAttribute("selected"))
                            {
                                result += (result == String.Empty ? String.Empty : ",") + e["value"];
                            }
                        }
                        return result;
                    default:
                        return e.GetAttribute("value");
                }
            }
            else
            {
                return String.Empty;
            }
        }
        /// <summary>
        /// Set the value of each element in the set of matched elements. If a comma-separated value is passed to a multuple select list, then it
        /// will be treated as an array.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Val(string value)
        {
            foreach (IDomElement e in Elements)
            {
                switch (e.NodeName)
                {
                    case "textarea":
                        // should we delete existing children first? they should not exist
                        e.InnerText = value;
                        break;
                    case "input":
                        switch (e.GetAttribute("type",String.Empty))
                        {
                            default:
                                e.SetAttribute("value", value);
                                break;
                        }
                        break;
                    case "select":
                        bool multiple = e.HasAttribute("multiple");
                        HashSet<string> values = null;
                        if (multiple)
                        {
                            values = new HashSet<string>(value.Split(','));
                        }
                        SetOptionSelected(e.Elements, value,values, multiple);
                        break;
                    default:
                        e.SetAttribute("value", value);
                        break;
                }

            }
            return this;
        }
        /// <summary>
        /// Helper function for option groups
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="value"></param>
        protected void SetOptionSelected(IEnumerable<IDomElement> elements, string value, HashSet<string> values, bool multiple)
        {

            foreach (IDomElement e in elements)
            {
                switch(e.NodeName) {
                    case "option":
                        if (multiple ? values.Contains(e["value"]) : e["value"] == value) {
                            e.SetAttribute("selected");
                        } else {
                            e.RemoveAttribute("selected");
                        }
                        break;
                    case "optgroup":
                        SetOptionSelected(e.Elements, value, values, multiple);
                        break;
                }
              
            }
        }
        /// <summary>
        /// Set the value of each mutiple select element in the set of matched elements. Any elements not of type &lt;SELECT multiple&gt;&lt;/SELECT&gt; will be ignored.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Val(IEnumerable<object> values) {
            string valueString=String.Empty;
            foreach (object val in values) {
                valueString+=(String.IsNullOrEmpty(val.ToString())?String.Empty:",") + val.ToString();
            }
            foreach (IDomElement e in Elements)
            {
                if (e.NodeName == "select" && e.HasAttribute("multiple"))
                {
                    Val(valueString);
                }
            }
            return this;
        }
        /// <summary>
        /// Set the CSS width of each element in the set of matched elements.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Width(int value)
        {
            return Width(value.ToString() + "px");
        }
        public CsQuery Width(string value)
        {
            return Css("width", value);
        }
        /// <summary>
        /// Set the CSS width of each element in the set of matched elements.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsQuery Height(int value)
        {
            return Height(value.ToString() + "px");
        }
        public CsQuery Height(string value)
        {
            return Css("height", value);
        }

        /// <summary>
        /// Check the current matched set of elements against a selector and return true if at least one of these elements matches the selector.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public bool Is(string selector)
        {
            return Filter(selector).Length > 0;
            //CsQuerySelectors selectors = new CsQuerySelectors(selector);
            //return !selectors.Select(Dom,Selection).IsNullOrEmpty();
        }
        public bool Is(IEnumerable<IDomObject> elements)
        {
            HashSet<IDomObject> els = new HashSet<IDomObject>(elements);
            els.IntersectWith(Selection);
            return els.Count > 0;
            //CsQuerySelectors selectors = new CsQuerySelectors(elements);
            //return !selectors.Select(Dom, Selection).IsNullOrEmpty();
        }
        public bool Is(IDomObject element)
        {
            return Selection.Contains(element);

            //CsQuerySelectors selectors = new CsQuerySelectors(element);
            //return !selectors.Select(Dom, Selection).IsNullOrEmpty();
        }

        public static object Extend(object target, object source1, object source2 = null, object source3 = null, object source4 = null, object source5 = null, object source6 = null, object source7 = null)
        {
            return CsQuery.Extend(false, target, source1, source2, source3, source4, source5, source6, source7);
        }
        public static object Extend(bool deep, object target, object source1, object source2 = null, object source3 = null, object source4 = null, object source5 = null, object source6 = null, object source7 = null)
        {
            return Utility.Objects.Extend(null,deep,target,source1,source2,source3,source4,source5,source6,source7);
        }
        
        /// <summary>
        /// Convert an object to JSON
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        public static string ToJSON(object objectToSerialize)
        {
            return Utility.JSON.ToJSON(objectToSerialize);
            
        }
        /// <summary>
        /// Parse JSON into a typed object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToDeserialize"></param>
        /// <returns></returns>
        public static T ParseJSON<T>(string objectToDeserialize)
        {

            return Utility.JSON.ParseJSON<T>(objectToDeserialize);
        }
        /// <summary>
        /// Parse JSON into an expando object
        /// </summary>
        /// <param name="objectToDeserialize"></param>
        /// <returns></returns>
        public static object ParseJSON(string objectToDeserialize)
        {
            return Utility.JSON.ParseJSON(objectToDeserialize);
        }

#endregion
        
        protected int GetElementIndex(IDomObject element) {
            int count = 0;
            foreach (IDomObject e in element.ParentNode.ChildNodes)
            {
                if (ReferenceEquals(e, element))
                {
                    break;
                }
                count++;
            }
            return count;
        }
        #region IEnumerable<IDomElement> Members

        public IEnumerator<IDomObject> GetEnumerator()
        {
            return Selection.GetEnumerator();
        }
        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Selection.GetEnumerator();
        }

        #endregion

    }


    
}