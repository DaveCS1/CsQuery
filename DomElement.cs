﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Web;
using Jtc.CsQuery.ExtensionMethods;

namespace Jtc.CsQuery
{
    [Flags]
    public enum DomRenderingOptions
    {
        RemoveMismatchedCloseTags = 1,
        RemoveComments = 2
    }
    public enum DocType
    {
        HTML5 = 1,
        HTML4 = 2,
        XHTML = 3,
        Unknown = 4
    }
    public enum NodeType
    {
        ELEMENT_NODE  =1,
        //ATTRIBUTE_NODE =2,
        TEXT_NODE = 3,
        CDATA_SECTION_NODE = 4,
        //ENTITY_REFERENCE_NODE = 5,
        //ENTITY_NODE=  6,
        //PROCESSING_INSTRUCTION_NODE =7,
        COMMENT_NODE =  8,
        DOCUMENT_NODE =  9,
        DOCUMENT_TYPE_NODE = 10,
        //DOCUMENT_FRAGMENT_NODE = 11,
        //NOTATION_NODE  =12
    }
    public interface IDomObject
    {

        IDomContainer ParentNode { get; set; }
        NodeType NodeType {get;}
        string PathID {get;}
        string Path { get; }
        CsQuery Owner { get; set; }
        DomRoot Dom { get; }
        string Render();
        void AddToIndex();
        void RemoveFromIndex();
        void Remove();
        IDomObject Clone();
        bool InnerHtmlAllowed {get;}
        string InnerHtml { get; set; }
        string InnerText { get; set; }
        bool Complete { get; }
        int DescendantCount();

        //? These are really only part of IDomContainer. However, to avoid awful typecasting all the time, they are part of the interface
        // for objects.

        string ID { get; set; }
        CSSStyleDeclaration Style { get; }
        DomAttributes Attributes { get; }
        string Class { get; }

        string NodeName { get; set; }
        string NodeValue { get; set; }
        IEnumerable<IDomElement> Elements { get; }
        NodeList ChildNodes { get; }
        IDomObject FirstChild { get; }
        IDomObject LastChild { get; }
        void AppendChild(IDomObject element);
        void RemoveChild(IDomObject element);

        void SetAttribute(string name);
        void SetAttribute(string name, string value);
        string GetAttribute(string name);
        string GetAttribute(string name, string defaultValue);
        bool TryGetAttribute(string name, out string value);
        bool HasAttribute(string name);
        bool RemoveAttribute(string name);

        bool Selected { get; }
        bool Checked { get; set; }
        bool ReadOnly { get; set; }

    }

    /// <summary>
    /// Defines an interface for elements whose defintion (not innerhtml) contain non-tag or attribute formed data
    /// </summary>
    public interface IDomSpecialElement: IDomObject 
    {
        string NonAttributeData { get; set; }
        
       
    }
    public interface IDomText : IDomObject
    {
        string Text { get; set; }
    }
    /// <summary>
    /// A marker interface an element that will be rendered as text because it was determined to be a mismatched tag
    /// </summary>
    public interface IDomInvalidElement : IDomText
    {
        
    }
    public interface IDomComment :  IDomSpecialElement
    {
        bool IsQuoted { get; set; }
    }
    public interface IDomCData :  IDomSpecialElement
    {
    }
    public interface IDomDocumentType :  IDomSpecialElement
    {
    }
    public interface IDomContainer : IDomObject
    {

        //void Insert(IDomObject element, int index);
        string GetNextChildID();
        
        IEnumerable<IDomObject> CloneChildren();

    }
    public interface IDomRoot : IDomContainer
    {
        DocType DocType { get; set; }
        DomRenderingOptions DomRenderingOptions { get; set; }
        IDomElement GetElementById(string id);

    }
    public interface IDomElement : IDomContainer
    {
        IEnumerable<string> Classes { get; }
        //IEnumerable<KeyValuePair<string, string>> Styles { get; }

        bool HasClass(string className);
        bool AddClass(string className); 
        bool RemoveClass(string className);

        void AddStyle(string styleString);
        bool RemoveStyle(string name);




        string this[string index] { get; set; }

        string ElementHtml { get; }

    }
    /// <summary>
    /// Base class for anything that exists in the DOM
    /// </summary>
    /// 
    public abstract class DomObject<T>: IDomObject where T: IDomObject,new()
    {
        public abstract bool InnerHtmlAllowed { get;}
        public virtual CsQuery Owner {
            get
            {
                return _Owner;
            }
            set
            {
                _Owner = value;
                if (this is IDomContainer)
                {
                    foreach (IDomObject obj in ((IDomContainer)this).ChildNodes)
                    {
                        obj.Owner = value;
                    }
                }
            }
        }
        protected CsQuery _Owner = null;
        public virtual DomRoot Dom
        {
            get
            {
                if (Owner != null)
                {
                    return Owner.Dom;
                }
                else
                {
                    return null;
                }
            }
        }

        public virtual string ID
        {
            get
            {
                return String.Empty;
            }
            set
            {
                throw new Exception("Cannot set ID for this node type.");
            }
        }
        public virtual string Class
        {
            get
            {
                return String.Empty;
            }
        }
        public virtual DomAttributes Attributes
        {
            get
            {
                throw new Exception("Attributes is not applicable to this node type.");
            }
        }
        public virtual CSSStyleDeclaration Style
        {
            get
            {
                throw new Exception("Style is not applicable to this node type.");
            }
        }
        public virtual string NodeName 
        {
            get {
                return String.Empty;
            }
            set
            {
                throw new Exception("You can't change the node name.");
            }
        }
        public virtual string NodeValue
        {
            get
            {
                return null;
            }
            set
            {
                throw new Exception("You can't set NodeValue for this node type.");
            }
        }
        public virtual string InnerHtml
        {
            get
            {
                return String.Empty;
            }
            set
            {
                throw new Exception("Assigning InnerHtml is not valid for this element type.");
            }
        }
        public virtual string InnerText
        {
            get
            {
                return String.Empty;
            }
            set
            {
                throw new Exception("Assigning InnerText is not valid for this element type.");
            }
        }
        // Owner can be null (this is an unbound element)
        // if so create an arbitrary one.

        public abstract NodeType NodeType { get; }
        public virtual T Clone()
        {
            T clone = new T();

            // prob should just implemnt this in the subclass but easier for now
            if (clone is IDomSpecialElement)
            {
                ((IDomSpecialElement)clone).NonAttributeData = ((IDomSpecialElement)this).NonAttributeData;
            }
            return clone;
        }

        // Unique ID assigned when added to a dom
        public string PathID
        {
            get
            {
                if (_PathID ==null) {

                    _PathID = (ParentNode == null ? String.Empty : ParentNode.GetNextChildID());
               }
               return _PathID;
            }

        } protected string _PathID = null;
        public string Path {
            get
            {
                if (_Path != null) {
                    return _Path;
                }
                return (ParentNode == null ? String.Empty : ParentNode.Path + "/") + PathID;
            }
        }
        protected string _Path = null;
        
        public IDomContainer ParentNode
        {
            get
            {
                return _Parent;
            }
            set
            {
                _Path = null;
                _PathID = null;
                _Parent = value;
            }
        }

        protected IDomContainer _Parent = null;
        public abstract bool Complete { get; }
        public abstract string Render();
        
        protected int IDCount = 0;

        protected IEnumerable<string> IndexKeys()
        {
            if (!(this is DomElement)) {
                yield break;
            }

            DomElement e = this as DomElement;
            if (!Complete)
            {
                throw new Exception("This element is incomplete and cannot be added to a DOM.");
            }
            // Add just the element to the index no matter what so we have an ordered representation of the dom traversal
            yield return IndexKey(String.Empty);
            yield return IndexKey(e.NodeName);
            if (!String.IsNullOrEmpty(e.ID))
            {
                yield return IndexKey("#" + e.ID);
            }
            foreach (string cls in e.Classes)
            {
                yield return IndexKey("." + cls);
            }
            //todo -add attributes?
        }
        protected int UniqueID = 0;
        /// <summary>
        /// Remove this element from the DOM
        /// </summary>
        public void Remove()
        {
            if (ParentNode == null)
            {
                throw new Exception("This element has no parent.");
            }
            ParentNode.ChildNodes.Remove(this);
        }
        public void AddToIndex()
        {
            if (Dom!=null && this is IDomElement)
            {
                // Fix the path when it's added to the index.
                // This is a little confusing. Would rather that we can't access it until it's added to a DOM.

                _Path = Path;
                foreach (string key in IndexKeys())
                {
                    AddToIndex(key);
                }
                if (this is IDomContainer)
                {
                    IDomContainer e = (IDomContainer)this;

                    foreach (IDomObject child in e.ChildNodes)
                    {
                        // Move root in case this is coming from an unmapped or alternate DOM
                        child.Owner = Owner;
                        child.AddToIndex();
                    }
                }
            }
        }
        public void RemoveFromIndex()
        {
            if (Dom != null && this is IDomElement)
            {
                if (this is IDomContainer)
                {
                    IDomContainer e = (IDomContainer)this;

                    foreach (DomElement child in e.Elements)
                    {
                        child.RemoveFromIndex();
                    }
                }
                foreach (string key in IndexKeys())
                {
                    RemoveFromIndex(key);
                }
            }
        }
        /// <summary>
        /// Remove only a single index, not the entire object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        public void RemoveFromIndex(string key)
        {
            if (Dom != null)
            {
                Dom.SelectorXref.Remove(key);
            }
        }
        public void AddToIndex(string key)
        {
            if (Dom != null)
            {
                Dom.SelectorXref.Add(key, this as DomElement);
            }
        }
        protected string IndexKey( string key)
        {
            return key + ">" + Path;
        }

        IDomObject IDomObject.Clone()
        {
            return Clone();
        }
        public virtual int DescendantCount()
        {
            return 0;
        }

        public virtual IEnumerable<IDomElement> Elements
        {
            get
            {
                yield break;
            }
        }
        public virtual NodeList ChildNodes
        {
            get
            {
                return null;
            }
        }
        public virtual IDomObject FirstChild
        {
            get { return null; }
        }
        public virtual IDomObject LastChild
        {
            get { return null; }
        }
        public virtual void AppendChild(IDomObject element)
        {
            throw new Exception("This type of element does not have children.");
        }
        public virtual void RemoveChild(IDomObject element)
        {
            throw new Exception("This type of element does not have children.");
        }


        public virtual void SetAttribute(string name)
        {
            return;
        }

        public virtual void SetAttribute(string name, string value)
        {
            return;
        }

        public virtual string GetAttribute(string name)
        {
            return null;
        }

        public virtual string GetAttribute(string name, string defaultValue)
        {
            return null;
        }

        public virtual bool TryGetAttribute(string name, out string value)
        {
            value = null;
            return false;
        }

        public virtual bool HasAttribute(string name)
        {
            return false;
        }

        public virtual bool RemoveAttribute(string name)
        {
            return false;
        }


        public virtual bool Selected
        {
            get { return false; }
        }

        public virtual bool Checked
        {
            get
            {
                return false;
            }
            set
            {
                throw new Exception("Not valid for this element type.");
            }
        }

        public virtual bool ReadOnly
        {
            get
            {
                return true;
            }
            set
            {
                throw new Exception("Not valid for this element type.");
            }
        }
    }
    
    /// <summary>
    /// Catch-all for unimplemented node types (e.g.
    /// </summary>
    
    public class DomDocumentType : DomObject<DomDocumentType>,IDomDocumentType 
    {
        public DomDocumentType()
            : base()
        {

        }
        public override NodeType NodeType
        {
            get { return NodeType.DOCUMENT_TYPE_NODE; }
        }
        public DocType DocType
        {
            get
            {
                if (_DocType != 0)
                {
                    return _DocType;
                }
                else
                {
                    return GetDocType();
                }
            }
            set
            {
                _DocType = value;
                Dom.DocType = value;
            }
        }
        protected DocType _DocType = 0;

        public override string Render()
        {
            return "<!DOCTYPE " + NonAttributeData + ">"; 
        }

        public string  NonAttributeData
        {
            get
            {
                if (_DocType == 0)
                {
                    return _NonAttributeData;
                }
                else
                {
                    switch (_DocType)
                    {
                        case DocType.HTML5:
                            return "html";
                        case DocType.XHTML:
                            return "html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"";
                        case DocType.HTML4:
                            return "html PUBLIC \"-//W3C//DTD HTML 4.01 Frameset//EN\" \"http://www.w3.org/TR/html4/frameset.dtd\"";
                        default:
                            throw new Exception("Unimplemented doctype");
                    }

                }
            }
	        set 
	        { 
		        _NonAttributeData = value;
	        }
        }
        protected string _NonAttributeData = String.Empty;
        protected DocType GetDocType()
        {
            string data = NonAttributeData.Trim().ToLower();
            if (data == "html")
            {
                return DocType.HTML5;
            } else if (data.IndexOf("xhtml 1")>=0) {
                return DocType.XHTML;
            }
            else if (data.IndexOf("html 4") >= 0)
            {
                return DocType.HTML4;
            }
            else
            {
                return DocType.Unknown;
            }
        }

        public override bool Complete
        {
            get { return true; }
        }
        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }
        public override string ToString()
        {
            return Render();
        }
        #region IDomSpecialElement Members

        public string Text
        {
            get
            {
                return NonAttributeData;
            }
            set
            {
                NonAttributeData = value;
            }
        }

        #endregion

        
  
    }
    public class DomCData : DomObject<DomCData>, IDomCData
    {
        public DomCData()
            : base()
        {

        }
        public override string NodeValue
        {
            get
            {
                return NonAttributeData;
            }
            set
            {
                NonAttributeData = value;
            }
        }
        public override NodeType NodeType
        {
            get { return NodeType.CDATA_SECTION_NODE; }
        }
        public override string Render()
        {
            return GetHtml(NonAttributeData);
        }
        protected string GetHtml(string innerText)
        {
            return "<![CDATA[" + innerText + ">";
        }
        public override string ToString()
        {
            string innerText = NonAttributeData.Length > 80 ? NonAttributeData.Substring(0, 80) + " ... " : NonAttributeData;
            return GetHtml(innerText);
        }
        #region IDomSpecialElement Members

        public string NonAttributeData
        {
            get;
            set;
        }
        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }
        public override bool Complete
        {
            get { return true; }
        }
        public string Text
        {
            get
            {
                return NonAttributeData;
            }
            set
            {
               NonAttributeData=value;
            }
        }

        #endregion
    }
    public class DomComment : DomObject<DomComment>, IDomComment 
    {
        public DomComment()
            : base()
        {
        }
        public DomComment(string text)
        {
            Text = text;
        }
        public override NodeType NodeType
        {
            get { return NodeType.COMMENT_NODE; }
        }
        public bool IsQuoted { get; set; }
        protected string TagOpener
        {
            get { return IsQuoted ? "<!--" : "<!"; }
        }
        protected string TagCloser
        {
            get { return IsQuoted ? "-->" : ">"; }
        }
        public override string Render()
        {
            if (Dom != null && Dom.DomRenderingOptions.HasFlag(DomRenderingOptions.RemoveComments))
            {
                return String.Empty;
            }
            else
            {
                return GetComment(NonAttributeData);
            }
        }
        protected string GetComment(string innerText)
        {
            return TagOpener + innerText + TagCloser;
        }

        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }
        public override bool Complete
        {
            get { return true; }
        }
        public override string ToString()
        {
            string innerText = NonAttributeData.Length > 80 ? NonAttributeData.Substring(0, 80) + " ... " : NonAttributeData;
            return GetComment(innerText);
        }
        #region IDomSpecialElement Members

        public string NonAttributeData
        {
            get;
            set;
        }

        public string Text
        {
            get
            {
                return NonAttributeData;
            }
            set
            {
                NonAttributeData = value;
            }
        }

        #endregion
    }
    /// <summary>
    /// Used for literal text (not part of a tag)
    /// </summary>
    public class DomText : DomObject<DomText>, IDomText
    {
        public DomText()
        {
            Initialize();
        }
        public DomText(string text): base()
        {
            Initialize();
            Text = text;
        }
        protected void Initialize()
        {
            Text = String.Empty;
        }
        public override NodeType NodeType
        {
            get { return NodeType.TEXT_NODE; }
        }
        public string Text
        {
            get;
            set;
        }
        public override string NodeValue
        {
            get
            {
                return Text;
            }
            set
            {
                Text = value;
            }
        }
        public override string Render()
        {
            return Text;
        }
        public override DomText Clone()
        {
            DomText domText = base.Clone();
            domText.Text = Text;
            return domText;
        }
        
        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }
        public override bool Complete
        {
            get { return !String.IsNullOrEmpty(Text);  }
        }
        public override string ToString()
        {
            return Text;
        }
        public override string InnerText
        {
            get
            {
                return HttpUtility.HtmlDecode(Text);
            }
            set
            {
                Text = HttpUtility.HtmlEncode(value);
            }
        }

    }

    public class DomInvalidElement : DomText, IDomInvalidElement
    {
        public DomInvalidElement()
            : base()
        {
        }
        public DomInvalidElement(string text): base(text)
        {

        }
        public override string Render()
        {
            if (Dom != null &&
                Dom.DomRenderingOptions.HasFlag(DomRenderingOptions.RemoveMismatchedCloseTags)) {
                return String.Empty;
            } else {
                return base.Render();
            }
        }
    }
    
    /// <summary>
    /// Base class for Dom object that contain other elements
    /// </summary>
    public abstract class DomContainer<T> : DomObject<T>, IDomContainer where T: IDomObject,IDomContainer, new()
    { 
        public DomContainer()
        {

        }
        
        public DomContainer(IEnumerable<IDomObject> elements)
        {
            ChildNodes.AddRange(elements);   
        }

        public abstract IEnumerable<IDomObject> CloneChildren();
        /// <summary>
        /// Returns all children (including inner HTML as objects);
        /// </summary>
        public override NodeList ChildNodes
        {
            get
            {
                if (_ChildNodes==null) {
                    _ChildNodes = new NodeList(this);
                }
                return _ChildNodes;
            }
        }
        protected NodeList _ChildNodes = null;
        public override IDomObject FirstChild
        {
            get
            {
                if (ChildNodes.Count > 0)
                {
                    return ChildNodes[0];
                }
                else
                {
                    return null;
                }
            }
        }
        public override IDomObject LastChild
        {
            get
            {
                if (ChildNodes.Count > 0)
                {
                    return ChildNodes[ChildNodes.Count - 1];
                }
                else
                {
                    return null;
                }
            }
        }
        public override void AppendChild(IDomObject item)
        {
            ChildNodes.Add(item);
        }
        public override void RemoveChild(IDomObject item)
        {
            ChildNodes.Remove(item);
        }
        /// <summary>
        /// Returns all elements
        /// </summary>
        public override IEnumerable<IDomElement> Elements
        {
            get
            {
                foreach (IDomObject elm in ChildNodes)
                {
                    if (elm is DomElement)
                    {
                        yield return (IDomElement)elm;
                    }
                }
                yield break;
            }
        }
        public IDomObject this[int index]
        {
            get
            {
                return ChildNodes[index];
            }
        }

        public override string Render()
        {
                StringBuilder sb = new StringBuilder();
                foreach (IDomObject e in ChildNodes)
                {
                    sb.Append(e.Render());
                }
                return (sb.ToString());
        } 


        /// </summary>
        /// <param name="elements"></param>

        


        //public override T Clone()
        //{
        //    T clone = base.Clone();
        //    foreach (IDomObject obj in _Children)
        //    {
        //        clone.Add(obj.Clone());
        //    }
        //    return clone;
        //}
        
        /// <summary>
        /// This is used to assign sequential IDs to children. Since they are requested by the children the method needs to be maintained in the parent.
        /// </summary>
        public string GetNextChildID()
        {
            
            return Base62Code(++IDCount);
            
        }
        // Just didn't use the / and the +. A three character ID will permit over 250,000 possible children at each level
        // so that should be plenty
        protected string Base62Code(int number)
        {
            string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            //string output = String.Empty;
            //int cur = 0;
            //int div = 0;
            //int mod;
            //int digit = 1;
            //do
            //{

            //    if (number >= 62)
            //    {
            //        div = (int)Math.Floor((float)(number / 62));
            //        mod = number - div * 62;
            //        if (mod>0) {


            //        cur = number - div;
            //        number -= div;
            //    }
            //    else
            //    {
            //        cur = number;
            //        number = -1;
            //    }
            //    output += chars[cur];
            //} while (number >= 0);
            //return output.PadLeft(3, '0');
            int ks_len = chars.Length;
            string sc_result = "";
            long num_to_encode = number;
            long i = 0;
            do
            {
                i++;
                sc_result = chars[(int)(num_to_encode % ks_len)] + sc_result;
                num_to_encode = ((num_to_encode - (num_to_encode % ks_len)) / ks_len);
            }
            while (num_to_encode != 0);
            return sc_result.PadLeft(3, '0');
        }
        public override int DescendantCount()
        {
            int count = 0;
            foreach (IDomObject obj in ChildNodes)
            {
                count += 1 + obj.DescendantCount();
            }
            return count;
        }
    }

    /// <summary>
    /// Special node type to represent the DOM.
    /// </summary>
    public class DomRoot : DomContainer<DomRoot>,IDomRoot 
    {
        public DomRoot()
            : base()
        {
        }
        public DomRoot(IEnumerable<IDomObject> elements)
            : base(elements)
        {

        }
        /// <summary>
        /// This is NOT INDEXED and should only be used for testing
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IDomElement GetElementById(string id)
        {
            return GetElementById(Elements, id);
        }
        protected IDomElement GetElementById(IEnumerable<IDomElement> elements, string id)
        {
            foreach (IDomElement el in elements)
            {
                if (el.ID == id)
                {
                    return el;
                }
                if (el.ChildNodes.Count>0) {
                    var childEl = GetElementById(el.Elements, id);
                    if (childEl != null)
                    {
                        return childEl;
                    }
                }
            }
            return null;
        }
        public DomRenderingOptions DomRenderingOptions
        { 
            get
            {
             return _DomRenderingOptions;
            } 
            set
            {
                _DomRenderingOptions=value;
            }
            

        }

        protected DomRenderingOptions _DomRenderingOptions = DomRenderingOptions.RemoveMismatchedCloseTags;
        public override DomRoot  Dom
        {
	          get 
	        { 
		         return this;
	        }
        }
        public override NodeType NodeType
        {
            get { return NodeType.DOCUMENT_NODE; }
        }
        public DomDocumentType  DocTypeNode {
            get
            {
                foreach (IDomObject obj in Dom.ChildNodes)
                {
                    if (obj.NodeType == NodeType.DOCUMENT_TYPE_NODE)
                    {
                        return (DomDocumentType)obj;
                    }
                }
                return null;
            }
        }
        /// <summary>
        /// Gets the DocType for this node. This can be changed through the DomRoot
        /// </summary>
        public DocType DocType
        {
            get
            {
                if (_DocType==0) {
                    DomDocumentType docType = DocTypeNode;
                    if (docType == null)
                    {
                        _DocType = DocType.XHTML;
                    }
                    else
                    {
                        _DocType = docType.DocType;
                    }
                }
                return _DocType;
            }
            set
            {
                // Keep synchronized with DocTypeNode
                if (_settingDocType) return;
                _settingDocType = true;
                _DocType = value;
                DomDocumentType docType = DocTypeNode;
                if (docType != null)
                {
                    DocTypeNode.DocType = value;
                }
                _settingDocType = false;
            
            }
        }
        private bool _settingDocType = false;
        protected DocType _DocType = 0;
        public RangeSortedDictionary<DomElement> SelectorXref = new RangeSortedDictionary<DomElement>();
        public override bool InnerHtmlAllowed
        {
            get { return true; }
        }
        public override bool Complete
        {
            get { return true; }
        }
        public override string ToString()
        {
            return "DOM Root (" + DocType.ToString()+", " + DescendantCount().ToString() + " elements)";
        }
        public override IEnumerable<IDomObject> CloneChildren()
        {
            foreach (IDomObject obj in ChildNodes)
            {
                yield return obj.Clone();
            }
        }
    }
    
    /// <summary>
    /// HTML elements
    /// </summary>
    public class DomElement : DomContainer<DomElement>, IDomElement
    {
        public DomElement()
        {
        }
        public DomElement(string tag)
        {
            NodeName = tag;
        }
        public override NodeType NodeType
        {
            get { return NodeType.ELEMENT_NODE; }
        }
        /// <summary>
        /// Creates a deep clone of this
        /// </summary>
        /// <returns></returns>
        public override DomElement Clone()
        {
            DomElement e = base.Clone();
            e.NodeName = NodeName;

            if (_Style != null)
            {
                e._Style = Style.Clone();
            }
            if (_Classes != null)
            {
                e._Classes = new HashSet<string>(Classes);
            }
            foreach (var attr in _Attributes)
            {
                e.SetAttribute(attr.Key, attr.Value);
            }
            e.ChildNodes.AddRange(CloneChildren());


            return e;
        }
        public  override IEnumerable<IDomObject> CloneChildren()
        {
            foreach (IDomObject obj in ChildNodes)
            {
                yield return obj.Clone();
            }
        }

        public IEnumerable<string> Classes
        {
            get
            {
                foreach (string val in _Classes)
                {
                    yield return val;
                }
            }
        } protected HashSet<string> _Classes = new HashSet<string>();

        public bool HasClass(string name)
        {
            return _Classes.Contains(name);
        }
        public bool AddClass(string name)
        {
            if (_Classes.Add(name))
            {
                AddToIndex(IndexKey("."+name));
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool RemoveClass(string name)
        {
            if (_Classes.Remove(name))
            {
                RemoveFromIndex(IndexKey("." + name));
            }
            return false;
        }

        /// <summary>
        /// Add a single style in the form "styleName: value"
        /// </summary>
        /// <param name="style"></param>
        public void AddStyle(string style)
        {
            AddStyle(style,true);
        }
        public void AddStyle(string style,bool raw)
        {
            int index = style.IndexOf(":");
            string stName;
            string stValue;
            if (index > 0)
            {
                stName = style.Substring(0, index).Trim();
                stValue = style.Substring(index + 1).Trim();
                if (raw)
                {
                    Style.SetRaw(stName, stValue);
                }
                else
                {
                    Style[stName] = stValue;
                }
            }
        }
        public bool RemoveStyle(string name)
        {
            return Style.Remove(name);
        }
        public override bool HasAttribute(string name)
        {
            string value;
            if (Attributes.TryGetValue(name.ToLower(), out value))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void SetStyles(string styles)
        {
            SetStyles(styles, true);
        }
        public void SetStyles(string styles, bool strict)
        {
            Style.Clear();
            string[] stList = styles.Trim().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string val in stList)
            {
                AddStyle(val,strict);
            }
        }
        /// <summary>
        /// Set the value of an attribute to "value." 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void SetAttribute(string name, string value)
        {
            name = name.ToLower();
            // TODO this is not right, should be able to set Class attribute, seaprate this handling
            switch (name)
            {
                case "class":
                    _Classes.Clear();
                    foreach (string val in value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _Classes.Add(val);
                    }
                    break;
                case "style":
                    // when setting "style" using SetAttribute, use raw mode.
                    SetStyles(value, true);
                    break;
                default:
                    Attributes[name] = value;
                    break;
            }
        }
        /// <summary>
        /// Sets an attribute with no value
        /// </summary>
        /// <param name="name"></param>
        public override void SetAttribute(string name)
        {
            SetAttribute(name, String.Empty);
        }
        public override bool RemoveAttribute(string name)
        {
            return Attributes.Remove(name.ToLower());
        }


        /// <summary>
        /// Gets an attribute value, or returns null if the value is missing. If a valueless attribute is found, this will also return null. HasAttribute should be used
        /// to test for such attributes. Attributes with an empty string value will return String.Empty.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override string GetAttribute(string name)
        {
            string defaultValue = null;
 
            if (name.Equals("value",StringComparison.CurrentCultureIgnoreCase) && 
                (NodeName == "input" || NodeName=="option" || NodeName=="select")) {
                defaultValue = String.Empty;
            }
            return GetAttribute(name, defaultValue);
        }
        /// <summary>
        /// Returns the value of an attribute or a default value if it could not be found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override string GetAttribute(string name, string defaultValue)
        {
            name = name.ToLower();
            string value = GetNonDictionaryAttribute(name);
            if (value != null)
            {
                return value;
            }
            if (Attributes.TryGetValue(name, out value))
            {
                 return value;
            }
            else
            {
                return defaultValue;
            }
        }
        public override bool TryGetAttribute(string name, out string value)
        {
            value = GetNonDictionaryAttribute(name);
            bool result = (GetNonDictionaryAttribute(name) != null) 
                || Attributes.TryGetValue(name.ToLower(), out  value);
            // use GetAttribute to actually get it because of all the exceptions. 
            value = GetAttribute(name);
            // even if the lookup failed, special attributes could return data in the main code
            return result || (value != null);
        }
        protected string GetNonDictionaryAttribute(string name)
        {
            switch (name)
            {
                case "style":
                    return Style.ToString();
                case "class":
                    return Class;
                default:
                    return null;
            }
        }
        public override CSSStyleDeclaration Style
        {
            get
            {
                if (_Style == null)
                {
                    _Style = new CSSStyleDeclaration();
                }
                return _Style;
            }
        }
        protected DomAttributes _Attributes = null;
        public override DomAttributes Attributes
        {
            get
            {
                if (_Attributes == null)
                {
                    _Attributes = new DomAttributes();
                }
                return _Attributes;
            }
        }
        protected CSSStyleDeclaration _Style = null;
        public override string Class
        {
            get
            {
                string cls = String.Empty;
                foreach (var val in _Classes)
                {
                    cls += (cls == String.Empty ? String.Empty : " ") + val;
                }
                return cls;
            }
        }

        public override string NodeName
        {
            get
            {
                return _Tag;
            }
            set
            {
                if (String.IsNullOrEmpty(NodeName))
                {
                    _Tag = value.ToLower();
                }
                else
                {
                    throw new Exception("You can't change the tag of an element once it has been created.");
                }
                
            }
        } protected string _Tag = null;
        public override string ID
        {
            get
            {
                return GetAttribute("id",String.Empty);
            }
            set
            {
                string id = _Attributes["id"];
                if (!String.IsNullOrEmpty(id))
                {
                    RemoveFromIndex(IndexKey("#" + id));
                }
                _Attributes["id"] = value;
                AddToIndex(IndexKey("#" + value));
            }
        }

        public string this[string name]
        {
            get
            {
                return GetAttribute(name);
            }
            set
            {
                SetAttribute(name, value);
            }
        }
        /// <summary>
        /// Returns text of the inner HTMl. When setting, any children will be removed.
        /// </summary>
        public override string InnerHtml
        {
            get
            {
                if (ChildNodes.IsNullOrEmpty())
                {
                    return String.Empty;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (IDomObject elm in ChildNodes)
                    {
                        sb.Append(elm.Render());
                    }
                    return sb.ToString();
                }
            }
            set
            {
                if (ChildNodes.Count > 0)
                {
                    ChildNodes.Clear();
                }
                CsQuery csq = CsQuery.Create(value);
                ChildNodes.AddRange(csq.Dom.ChildNodes);
            }
        }
        public override string InnerText
        {
            get
            {
                if (ChildNodes.IsNullOrEmpty())
                {
                    return String.Empty;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (IDomObject elm in ChildNodes)
                    {
                        if (elm is IDomText)
                        {
                            sb.Append(elm.Render());
                        }
                    }
                    return sb.ToString();
                }
            }
            set
            {
                if (ChildNodes.Count > 0)
                {
                    ChildNodes.Clear();
                }
                DomText text = new DomText(value);
                ChildNodes.Add(text);
            }
        }

        public override bool Selected
        {
            get
            {
                return HasAttribute("selected");
            }
        }
        public override bool Checked
        {
            get
            {
                return HasAttribute("checked");
            }
            set
            {
                SetAttribute("checked");
            }
        }
        public override bool ReadOnly
        {
            get
            {
                return HasAttribute("readonly");
            }
            set
            {
                SetAttribute("readonly");
            }
        }
        /// <summary>
        /// Returns the completel HTML for this element and its children
        /// </summary>
        public override string Render()
        {
            return GetHtml(true);
        }
        /// <summary>
        /// Returns the HTML for this element, ignoring children/innerHTML
        /// </summary>
        public string ElementHtml
        {
            get
            {
                return GetHtml(false);
            }
        }
        protected DocType DocType
        {
            get
            {
                if (_DocType == 0)
                {
                    _DocType = Dom.DocType;
                }
                return _DocType;
            }
        }
        private DocType _DocType;
        
        char[] needsQuoting = new char[] {' ', '\'', '"'};
        protected string GetHtml(bool includeChildren)
        {
            
            StringBuilder sb = new StringBuilder();
            sb.Append("<" + NodeName);
            if (_Classes.Count > 0)
            {
                //if (_Classes.Count == 1 && DocType != DocType.XHTML)
                //{
                //    sb.Append(" class=" + Class);
                //}
                //else
               // {
                    sb.Append(" class=\"" + Class + "\"");
                //}
            }
            if (Style.Count > 0)
            {
                sb.Append(" style=\"" + Style.ToString() +"\"");
            }

            foreach (var kvp in _Attributes)
            {
                string val = kvp.Value;
                if (!String.IsNullOrEmpty(val))
                {
                    //if (DocType== DocType.XHTML || val.IndexOfAny(needsQuoting) >=0) {
                        string quoteChar = val.IndexOf("\"") >= 0 ? "'" : "\"";
                        sb.Append(" " + kvp.Key + "=" + quoteChar + val + quoteChar);
                    //} else {
                    //    sb.Append(" " + kvp.Key + "=" + val);
                   // }
                }
                else
                {
                    sb.Append(" " + kvp.Key);
                }
            }

            if (InnerHtmlAllowed)
            {
                sb.Append(String.Format(">{0}</" + NodeName + ">",
                    includeChildren ? InnerHtml :
                    (ChildNodes.Count > 0 ? "..." : String.Empty)
                    ));
            }
            else
            {
                if (DocType == DocType.XHTML)
                {
                    sb.Append(" />");
                }
                else
                {
                    sb.Append(" >");
                }
            }
            return sb.ToString();
        }
        
        public override string ToString()
        {
            return ElementHtml;
        }

        /// <summary>
        /// This object type can have inner HTML.
        /// </summary>
        /// <returns></returns>
        public override bool InnerHtmlAllowed
        {
            get
            {
                switch (NodeName.ToLower())
                {
                    case "base":
                    case "basefont":
                    case "frame":
                    case "link":
                    case "meta":
                    case "area":
                    case "col":
                    case "hr":
                    case "param":
                    case "input":
                    case "img":
                    case "br":
                    case "!doctype":
                    case "!--":
                        return false;
                    default:
                        return true;
                }
            }
        }
        public override bool Complete
        {
            get { return !String.IsNullOrEmpty(NodeName); }
        }
    }
}