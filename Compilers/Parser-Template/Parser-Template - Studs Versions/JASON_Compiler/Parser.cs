using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JASON_Compiler
{
    public class Node
    {
        public List<Node> Children = new List<Node>();

        public string Name;
        public Node(string N)
        {
            this.Name = N;
        }
    }

    public class Parser
    {
        int InputPointer = 0;
        List<Token> TokenStream;
        public Node root;

        public Node StartParsing(List<Token> TokenStream)
        {
            this.InputPointer = 0;
            this.TokenStream = TokenStream;
            root = Program();
            return root;
        }

        // ─────────────────────────────────────────────
        // 1. High-Level Program Structure
        // ─────────────────────────────────────────────

        // Program → Function_Statements Main_Function
        Node Program()
        {
            Node program = new Node("Program");
            program.Children.Add(Function_Statements());
            program.Children.Add(Main_Function());
            // Add at the end of Program(), before the MessageBox:
            if (InputPointer < TokenStream.Count)
            {
                Errors.Error_List.Add("Parsing Error: Unexpected tokens after end of program: "
                    + CurrentTokenName() + "\r\n");
            }
            //MessageBox.Show("Success");
            if (Errors.Error_List.Count == 0)
                MessageBox.Show("Parsing Successful!");
            else
                MessageBox.Show($"Parsing completed with {Errors.Error_List.Count} error(s).");
            return program;
        }

        // Function_Statements → Function_Statement Function_Statements | ε
        Node Function_Statements()
        {
            Node node = new Node("Function_Statements");

            // A Function_Statement starts with a Datatype keyword followed by an
            // identifier (NOT "main"). We peek two tokens ahead to distinguish from Main_Function.
            while (IsDatatype() && !PeekIsMain())
            {
                node.Children.Add(Function_Statement());
            }
            return node;
        }

        // Function_Statement → Function_Declaration Function_Body
        Node Function_Statement()
        {
            Node node = new Node("Function_Statement");
            node.Children.Add(Function_Declaration());
            node.Children.Add(Function_Body());
            return node;
        }

        // Main_Function → Datatype main () Function_Body
        Node Main_Function()
        {
            Node node = new Node("Main_Function");
            node.Children.Add(Datatype());
            node.Children.Add(match(Token_Class.Main));
            node.Children.Add(match(Token_Class.LParanthesis));
            node.Children.Add(match(Token_Class.RParanthesis));
            node.Children.Add(Function_Body());
            return node;
        }

        // Function_Declaration → Datatype identifier ( Parameters )
        Node Function_Declaration()
        {
            Node node = new Node("Function_Declaration");
            node.Children.Add(Datatype());
            node.Children.Add(match(Token_Class.Identifier));
            node.Children.Add(match(Token_Class.LParanthesis));
            node.Children.Add(Parameters());
            node.Children.Add(match(Token_Class.RParanthesis));
            return node;
        }

        // Parameters → Parameter Parameter_List | ε
        Node Parameters()
        {
            Node node = new Node("Parameters");
            if (IsDatatype())
            {
                node.Children.Add(Parameter());
                node.Children.Add(Parameter_List());
            }
            return node;
        }

        // Parameter_List → , Parameter Parameter_List | ε
        Node Parameter_List()
        {
            Node node = new Node("Parameter_List");
            while (Check(Token_Class.Comma))
            {
                node.Children.Add(match(Token_Class.Comma));
                node.Children.Add(Parameter());
            }
            return node;
        }

        // Parameter → Datatype identifier
        Node Parameter()
        {
            Node node = new Node("Parameter");
            node.Children.Add(Datatype());
            node.Children.Add(match(Token_Class.Identifier));
            return node;
        }

        // Function_Body → { Statements Return_Statement }
        Node Function_Body()
        {
            Node node = new Node("Function_Body");
            node.Children.Add(match(Token_Class.LCurly));
            node.Children.Add(Statements());
            node.Children.Add(Return_Statement());
            node.Children.Add(match(Token_Class.RCurly));
            return node;
        }

        // ─────────────────────────────────────────────
        // 2. Statements
        // ─────────────────────────────────────────────

        // Statements → Statement Statements | ε
        Node Statements()
        {
            Node node = new Node("Statements");
            while (IsStatementStart())
            {
                node.Children.Add(Statement());
            }
            return node;
        }

        // Statement → Declaration_Statement | Assignment_Statement | Write_Statement
        //           | Read_Statement | If_Statement | Repeat_Statement
        Node Statement()
        {
            Node node = new Node("Statement");

            if (IsDatatype())
            {
                node.Children.Add(Declaration_Statement());
            }
            else if (Check(Token_Class.Identifier))
            {
                node.Children.Add(Assignment_Statement());
            }
            else if (Check(Token_Class.Write))
            {
                node.Children.Add(Write_Statement());
            }
            else if (Check(Token_Class.Read))
            {
                node.Children.Add(Read_Statement());
            }
            else if (Check(Token_Class.If))
            {
                node.Children.Add(If_Statement());
            }
            else if (Check(Token_Class.Repeat))
            {
                node.Children.Add(Repeat_Statement());
            }
            else
            {
                Errors.Error_List.Add("Parsing Error: Unexpected token in Statement: " +
                    CurrentTokenName() + "\r\n");
                InputPointer++;
            }
            return node;
        }

        // Declaration_Statement → Datatype Decl_List ;
        Node Declaration_Statement()
        {
            Node node = new Node("Declaration_Statement");
            node.Children.Add(Datatype());
            node.Children.Add(Decl_List());
            node.Children.Add(match(Token_Class.Semicolon));
            return node;
        }

        // Decl_List → Decl_Item Decl_List'
        Node Decl_List()
        {
            Node node = new Node("Decl_List");
            node.Children.Add(Decl_Item());
            node.Children.Add(Decl_List_Prime());
            return node;
        }

        // Decl_List' → , Decl_Item Decl_List' | ε
        Node Decl_List_Prime()
        {
            Node node = new Node("Decl_List'");
            while (Check(Token_Class.Comma))
            {
                node.Children.Add(match(Token_Class.Comma));
                node.Children.Add(Decl_Item());
            }
            return node;
        }

        // Decl_Item → identifier Decl_Item'
        Node Decl_Item()
        {
            Node node = new Node("Decl_Item");
            node.Children.Add(match(Token_Class.Identifier));
            node.Children.Add(Decl_Item_Prime());
            return node;
        }

        // Decl_Item' → := Expression | ε
        Node Decl_Item_Prime()
        {
            Node node = new Node("Decl_Item'");
            if (Check(Token_Class.AssignOp))
            {
                node.Children.Add(match(Token_Class.AssignOp));
                node.Children.Add(Expression());
            }
            return node;
        }

        // Assignment_Statement → identifier := Expression
        Node Assignment_Statement()
        {
            Node node = new Node("Assignment_Statement");
            node.Children.Add(match(Token_Class.Identifier));
            node.Children.Add(match(Token_Class.AssignOp));
            node.Children.Add(Expression());
            // should not expect a semicolon but we put it here bec it's expected in the other statements (works with test2, doesn't with 1)
            node.Children.Add(match(Token_Class.Semicolon));

            return node;
        }

        // Write_Statement → write Write_Value ;
        Node Write_Statement()
        {
            Node node = new Node("Write_Statement");
            node.Children.Add(match(Token_Class.Write));
            node.Children.Add(Write_Value());
            node.Children.Add(match(Token_Class.Semicolon));
            return node;
        }

        // Write_Value → Expression | endl
        Node Write_Value()
        {
            Node node = new Node("Write_Value");
            if (Check(Token_Class.Endl))
            {
                node.Children.Add(match(Token_Class.Endl));
            }
            else
            {
                node.Children.Add(Expression());
            }
            return node;
        }

        // Read_Statement → read identifier ;
        Node Read_Statement()
        {
            Node node = new Node("Read_Statement");
            node.Children.Add(match(Token_Class.Read));
            node.Children.Add(match(Token_Class.Identifier));
            node.Children.Add(match(Token_Class.Semicolon));
            return node;
        }

        // Return_Statement → return Expression ;
        Node Return_Statement()
        {
            Node node = new Node("Return_Statement");
            node.Children.Add(match(Token_Class.Return));
            node.Children.Add(Expression());
            node.Children.Add(match(Token_Class.Semicolon));
            return node;
        }

        // Repeat_Statement → repeat Statements until Condition_Statement
        Node Repeat_Statement()
        {
            Node node = new Node("Repeat_Statement");
            node.Children.Add(match(Token_Class.Repeat));
            node.Children.Add(Statements());
            node.Children.Add(match(Token_Class.Until));
            node.Children.Add(Condition_Statement());
            return node;
        }

        // ─────────────────────────────────────────────
        // 3. Control Flow
        // ─────────────────────────────────────────────

        // If_Statement → if Condition_Statement then Statements If_Tail
        Node If_Statement()
        {
            Node node = new Node("If_Statement");
            node.Children.Add(match(Token_Class.If));
            node.Children.Add(Condition_Statement());
            node.Children.Add(match(Token_Class.Then));
            node.Children.Add(Statements());
            node.Children.Add(If_Tail());
            return node;
        }

        // If_Tail → Else_If_Statement | Else_Statement | end
        Node If_Tail()
        {
            Node node = new Node("If_Tail");
            if (Check(Token_Class.ElseIf))
            {
                node.Children.Add(Else_If_Statement());
            }
            else if (Check(Token_Class.Else))
            {
                node.Children.Add(Else_Statement());
            }
            else
            {
                node.Children.Add(match(Token_Class.End));
            }
            return node;
        }

        // Else_If_Statement → elseif Condition_Statement then Statements If_Tail
        Node Else_If_Statement()
        {
            Node node = new Node("Else_If_Statement");
            node.Children.Add(match(Token_Class.ElseIf));
            node.Children.Add(Condition_Statement());
            node.Children.Add(match(Token_Class.Then));
            node.Children.Add(Statements());
            node.Children.Add(If_Tail());
            return node;
        }

        // Else_Statement → else Statements end
        Node Else_Statement()
        {
            Node node = new Node("Else_Statement");
            node.Children.Add(match(Token_Class.Else));
            node.Children.Add(Statements());
            node.Children.Add(match(Token_Class.End));
            return node;
        }

        // ─────────────────────────────────────────────
        // 4. Expressions and Logic
        // ─────────────────────────────────────────────

        // Condition_Statement → Or_Condition
        Node Condition_Statement()
        {
            Node node = new Node("Condition_Statement");
            node.Children.Add(Or_Condition());
            return node;
        }

        // Or_Condition → And_Condition Or_Condition'
        Node Or_Condition()
        {
            Node node = new Node("Or_Condition");
            node.Children.Add(And_Condition());
            node.Children.Add(Or_Condition_Prime());
            return node;
        }

        // Or_Condition' → || And_Condition Or_Condition' | ε
        Node Or_Condition_Prime()
        {
            Node node = new Node("Or_Condition'");
            while (Check(Token_Class.OrOp))
            {
                node.Children.Add(match(Token_Class.OrOp));
                node.Children.Add(And_Condition());
            }
            return node;
        }

        // And_Condition → Condition And_Condition'
        Node And_Condition()
        {
            Node node = new Node("And_Condition");
            node.Children.Add(Condition());
            node.Children.Add(And_Condition_Prime());
            return node;
        }

        // And_Condition' → && Condition And_Condition' | ε
        Node And_Condition_Prime()
        {
            Node node = new Node("And_Condition'");
            while (Check(Token_Class.AndOp))
            {
                node.Children.Add(match(Token_Class.AndOp));
                node.Children.Add(Condition());
            }
            return node;
        }

        // Condition → identifier Condition_Operator Term
        Node Condition()
        {
            Node node = new Node("Condition");
            node.Children.Add(match(Token_Class.Identifier));
            node.Children.Add(Condition_Operator());
            node.Children.Add(Term());
            return node;
        }

        // Condition_Operator → < | > | = | <>
        Node Condition_Operator()
        {
            Node node = new Node("Condition_Operator");
            if (Check(Token_Class.LessThanOp))
                node.Children.Add(match(Token_Class.LessThanOp));
            else if (Check(Token_Class.GreaterThanOp))
                node.Children.Add(match(Token_Class.GreaterThanOp));
            else if (Check(Token_Class.EqualOp))
                node.Children.Add(match(Token_Class.EqualOp));
            else if (Check(Token_Class.NotEqualOp))
                node.Children.Add(match(Token_Class.NotEqualOp));
            else
            {
                Errors.Error_List.Add("Parsing Error: Expected a condition operator but found: " +
                    CurrentTokenName() + "\r\n");
                InputPointer++;
            }
            return node;
        }

        // Expression → stringLiteral | Equation
        Node Expression()
        {
            Node node = new Node("Expression");
            if (Check(Token_Class.StringLiteral))
            {
                node.Children.Add(match(Token_Class.StringLiteral));
            }
            else
            {
                node.Children.Add(Equation());
            }
            return node;
        }

        // Equation → Multiplicative_Expr Equation'
        Node Equation()
        {
            Node node = new Node("Equation");
            node.Children.Add(Multiplicative_Expr());
            node.Children.Add(Equation_Prime());
            return node;
        }

        // Equation' → Add_Operator Multiplicative_Expr Equation' | ε
        Node Equation_Prime()
        {
            Node node = new Node("Equation'");
            while (Check(Token_Class.PlusOp) || Check(Token_Class.MinusOp))
            {
                node.Children.Add(Add_Operator());
                node.Children.Add(Multiplicative_Expr());
            }
            return node;
        }

        // Multiplicative_Expr → Primary_Expr Multiplicative_Expr'
        Node Multiplicative_Expr()
        {
            Node node = new Node("Multiplicative_Expr");
            node.Children.Add(Primary_Expr());
            node.Children.Add(Multiplicative_Expr_Prime());
            return node;
        }

        // Multiplicative_Expr' → Mul_Operator Primary_Expr Multiplicative_Expr' | ε
        Node Multiplicative_Expr_Prime()
        {
            Node node = new Node("Multiplicative_Expr'");
            while (Check(Token_Class.MultiplyOp) || Check(Token_Class.DivideOp))
            {
                node.Children.Add(Mul_Operator());
                node.Children.Add(Primary_Expr());
            }
            return node;
        }

        // Primary_Expr → Term | ( Equation )
        Node Primary_Expr()
        {
            Node node = new Node("Primary_Expr");
            if (Check(Token_Class.LParanthesis))
            {
                node.Children.Add(match(Token_Class.LParanthesis));
                node.Children.Add(Equation());
                node.Children.Add(match(Token_Class.RParanthesis));
            }
            else
            {
                node.Children.Add(Term());
            }
            return node;
        }

        // Term → constant | identifier Term'
        Node Term()
        {
            Node node = new Node("Term");
            if (Check(Token_Class.Constant))
            {
                node.Children.Add(match(Token_Class.Constant));
            }
            else
            {
                node.Children.Add(match(Token_Class.Identifier));
                node.Children.Add(Term_Prime());
            }
            return node;
        }

        // Term' → ( Arg_List ) | ε
        Node Term_Prime()
        {
            Node node = new Node("Term'");
            if (Check(Token_Class.LParanthesis))
            {
                node.Children.Add(match(Token_Class.LParanthesis));
                node.Children.Add(Arg_List());
                node.Children.Add(match(Token_Class.RParanthesis));
            }
            return node;
        }

        // Arg_List → identifier Arg_Tail | ε
        Node Arg_List()
        {
            Node node = new Node("Arg_List");
            if (Check(Token_Class.Identifier))
            {
                node.Children.Add(match(Token_Class.Identifier));
                node.Children.Add(Arg_Tail());
            }
            return node;
        }

        // Arg_Tail → , identifier Arg_Tail | ε
        Node Arg_Tail()
        {
            Node node = new Node("Arg_Tail");
            while (Check(Token_Class.Comma))
            {
                node.Children.Add(match(Token_Class.Comma));
                node.Children.Add(match(Token_Class.Identifier));
            }
            return node;
        }

        // Add_Operator → + | -
        Node Add_Operator()
        {
            Node node = new Node("Add_Operator");
            if (Check(Token_Class.PlusOp))
                node.Children.Add(match(Token_Class.PlusOp));
            else
                node.Children.Add(match(Token_Class.MinusOp));
            return node;
        }

        // Mul_Operator → * | /
        Node Mul_Operator()
        {
            Node node = new Node("Mul_Operator");
            if (Check(Token_Class.MultiplyOp))
                node.Children.Add(match(Token_Class.MultiplyOp));
            else
                node.Children.Add(match(Token_Class.DivideOp));
            return node;
        }

        // Datatype → int | float | string
        Node Datatype()
        {
            Node node = new Node("Datatype");
            if (Check(Token_Class.Int))
                node.Children.Add(match(Token_Class.Int));
            else if (Check(Token_Class.Float))
                node.Children.Add(match(Token_Class.Float));
            else if (Check(Token_Class.String))
                node.Children.Add(match(Token_Class.String));
            else
            {
                Errors.Error_List.Add("Parsing Error: Expected a datatype (int/float/string) but found: " +
                    CurrentTokenName() + "\r\n");
                InputPointer++;
            }
            return node;
        }

        // ─────────────────────────────────────────────
        // Helper Utilities
        // ─────────────────────────────────────────────

        /// <summary>Returns true if the current token matches the given class (without consuming).</summary>
        bool Check(Token_Class tc)
        {
            return InputPointer < TokenStream.Count &&
                   TokenStream[InputPointer].token_type == tc;
        }

        /// <summary>Returns true if the current token is a datatype keyword.</summary>
        bool IsDatatype()
        {
            return Check(Token_Class.Int) ||
                   Check(Token_Class.Float) ||
                   Check(Token_Class.String);
        }

        /// <summary>
        /// Returns true when the current token is a datatype AND the next token is "main",
        /// meaning we are looking at the Main_Function, not a regular Function_Statement.
        /// </summary>
        bool PeekIsMain()
        {
            return InputPointer + 1 < TokenStream.Count &&
                   TokenStream[InputPointer + 1].token_type == Token_Class.Main;
        }

        /// <summary>
        /// Returns true if the current token can begin a Statement.
        /// Datatype → Declaration; identifier → Assignment; keywords for the rest.
        /// We also exclude tokens that belong to the enclosing construct's follow set.
        /// </summary>
        bool IsStatementStart()
        {
            if (InputPointer >= TokenStream.Count) return false;
            Token_Class tc = TokenStream[InputPointer].token_type;
            return tc == Token_Class.Int ||
                   tc == Token_Class.Float ||
                   tc == Token_Class.String ||
                   tc == Token_Class.Identifier ||
                   tc == Token_Class.Write ||
                   tc == Token_Class.Read ||
                   tc == Token_Class.If ||
                   tc == Token_Class.Repeat;
        }

        /// <summary>Returns the name of the current token for error messages.</summary>
        string CurrentTokenName()
        {
            if (InputPointer < TokenStream.Count)
                return TokenStream[InputPointer].token_type.ToString();
            return "EOF";
        }

        // ─────────────────────────────────────────────
        // match & tree printing (unchanged from scaffold)
        // ─────────────────────────────────────────────

        public Node match(Token_Class ExpectedToken)
        {
            if (InputPointer < TokenStream.Count)
            {
                if (ExpectedToken == TokenStream[InputPointer].token_type)
                {
                    Node newNode = new Node(TokenStream[InputPointer].lex ?? ExpectedToken.ToString());
                    InputPointer++;
                    return newNode;
                }
                else
                {
                    Errors.Error_List.Add("Parsing Error: Expected "
                        + ExpectedToken.ToString() + " and " +
                        TokenStream[InputPointer].token_type.ToString() +
                        "  found\r\n");
                    InputPointer++;
                    return null;
                }
            }
            else
            {
                Errors.Error_List.Add("Parsing Error: Expected "
                        + ExpectedToken.ToString() + "\r\n");
                InputPointer++;
                return null;
            }
        }

        public static TreeNode PrintParseTree(Node root)
        {
            TreeNode tree = new TreeNode("Parse Tree");
            TreeNode treeRoot = PrintTree(root);
            if (treeRoot != null)
                tree.Nodes.Add(treeRoot);
            return tree;
        }

        static TreeNode PrintTree(Node root)
        {
            if (root == null || root.Name == null)
                return null;
            TreeNode tree = new TreeNode(root.Name);
            if (root.Children.Count == 0)
                return tree;
            foreach (Node child in root.Children)
            {
                if (child == null)
                    continue;
                tree.Nodes.Add(PrintTree(child));
            }
            return tree;
        }
    }
}
