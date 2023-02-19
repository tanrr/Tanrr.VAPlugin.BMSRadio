using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;


namespace Tanrr.SimpleParser
{
	public class SimParContainedMember
	{
		protected string _name;             // Name to match in SimParBase
		public string Name					// NOTE: Any difference, especially different contained members or sequence, requires a different name
        { get => _name; }

		protected bool _required;			// MUST have the number given of this member if given
		public bool Required
		{ get => _required; }
		protected int _count;				// _required with _count==0 means AT LEAST ONE
		public int Count                    // _required with _count==N means EXACTLY N
		{ get => _count; }					// !_required with _count==0 means any number
											// !_required with _count==N means UP TO a count of N
		public SimParContainedMember(string name, bool required, int count = 1)
		{
			_name = name;
			_required = required;
			_count = count;
		}

        // Copy constructor.
        public SimParContainedMember(SimParContainedMember other) 
			: this(other._name, other._required, other._count)
		{ }
    }

    public class SimParBase
    {
		protected bool _simple;          // Matching very simple: name "value" or just "value"
		protected bool _noNameToParse;	// Though we have a stored name, it won't be in the file to parse
											// so we're just looking for "value"

        protected string _name;             // string (or pattern) we match looking for this token
        public string Name					// NOTE: Any difference, especially different contained members or sequence, requires a different name
        { get => _name; }

        protected string _value;			// Quoted text field (possibly before [] if not _simple)

        public SimParBase(bool simple, bool noNameToParse, string name)
        {
            _simple = simple;
            _noNameToParse = noNameToParse;
            _name = name;
            _value = string.Empty;
        }

		public SimParBase(SimParBase other)
		{
			_simple= other._simple;
			_noNameToParse= other._noNameToParse;
			_name = other._name;
			_value = other._value;
		}

    }

    public class SimParElement : SimParBase
	{
		protected bool _topLevel;			// Can't be contained in anything else - though multiple sequential possible
        protected bool _valueRequired;		// Must have a value (text string, before [] if subElements)
        protected bool _noValueAllowed;		// Must NOT have a value (!_valueRequired && !_noValueAllowed) = optional



		protected bool _brackets;             // If [] contain this element's members beyond its value string
												// NOTE: Must be true if multiple members, or members with their own brackets
		protected bool _multipleMembersOK;   // More than a single member is allowed (brackets required)

		protected SimParContainedMember[] _containedMembers;	// IN SEQUENCE list of possible contained members
													// Record an error if > 1 required member and parent has no _brackets
													// Record an error if any member with brackets if parent doesn't have brackets
													// Note that these can't be checked till after we've inited all members

		protected void InitChildMembers(
            bool topLevel,
            bool valueRequired,
            bool noValueAllowed,
            bool brackets,
            bool multipleMembersOK,
			SimParContainedMember[] containedMembers)
		{
			_topLevel = topLevel;
			_valueRequired = valueRequired;
			_noValueAllowed = noValueAllowed;
			_brackets = brackets;
			_multipleMembersOK = multipleMembersOK;
			// Need to make a copy of the contained members, if not null
			_containedMembers = null;
			if (containedMembers != null && containedMembers.Length > 0)
			{
				_containedMembers = new SimParContainedMember[containedMembers.Length];
				containedMembers.CopyTo(_containedMembers, 0 );
			}
		}

        public SimParElement(bool simple, bool noNameToParse, string name)
			: base(simple, noNameToParse, name)
		{
            InitChildMembers(
				topLevel: false, 
				valueRequired: true, 
				noValueAllowed: false, 
				brackets: false, 
				multipleMembersOK: false,
				containedMembers: null);
		}

        public SimParElement(
			bool simple,
			bool noNameToParse,
			string name,
			bool topLevel, 
			bool valueRequired, 
			bool noValueAllowed, 
			bool brackets,  
			bool multipleMembersOK, 
			SimParContainedMember[] containedMembers = null)
			: base( simple, noNameToParse, name)
		{
			InitChildMembers(topLevel, valueRequired, noValueAllowed, brackets, multipleMembersOK, containedMembers);

        }

		public SimParElement(SimParElement other)
			: base(other)
		{
			InitChildMembers(	other._topLevel, 
								other._valueRequired, 
								other._noValueAllowed, 
								other._brackets, 
								other._multipleMembersOK, 
								other._containedMembers);
        }
    }

	public class  SimParser
	{
		protected string _filepath;
        protected string _fileName;

		protected List<SimParContainedMember> _contTopMembers;			// Pattern sequence of the top level members this parser will match

		protected Dictionary<string, SimParElement> _simParElements;

        public SimParser(string filepath, string fileName)
		{
			_filepath = filepath;
			_fileName = fileName;
			_contTopMembers = new List<SimParContainedMember>();
			_simParElements = new Dictionary<string, SimParElement>();
		}

        public bool AddParserElementDef(SimParElement parElement)
        {
            if (parElement == null)
                return false;

            if (_simParElements.ContainsKey(parElement.Name))
            {
                return false;   // No duplicates allowed
            }

			SimParElement copyParElement = new SimParElement(parElement);

            _simParElements.Add(copyParElement.Name, copyParElement);

            return true;
        }

        public bool AddContainedTopMember(SimParContainedMember topMember)
		{
			if (topMember == null)
				return false;

			if (_simParElements.ContainsKey(topMember.Name))
			{
                // No other verification to do between SimParContainedMember and SimParElement yet
            }
            else
			{
				return false;	// Only allowed to add members that have matching keys
			}
			
            SimParContainedMember copyTopMember = new SimParContainedMember(topMember);

			_contTopMembers.Add(copyTopMember);

			return true;
		}

		public bool Parse()
		{
            int counter = 0;
			if (_contTopMembers.Count <= 0) 
			{
				// No pattern to parse with
				return false;
			}

			// Read the file line by line
			string fullPath = _filepath + _fileName;
			if (!System.IO.File.Exists(fullPath))
				return false;

            foreach (string line in System.IO.File.ReadLines(@fullPath))
            {
                System.Console.WriteLine(line);
                counter++;
            }
			return true;
        }

	}


} // Tanrr.SimpleParser namespace