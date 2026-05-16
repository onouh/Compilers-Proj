using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

//  escape quotes in strings are accepted
// starting and trailing dots in numbers are not accepted
public enum Token_Class
{
    // resereved words  
    Int, Float, String, Read, Write, Repeat, Until, If, ElseIf, Else, Then, Return, Endl, End, Main,
    // operators and punctuation
    PlusOp, MinusOp, MultiplyOp, DivideOp, LessThanOp, GreaterThanOp, EqualOp, NotEqualOp,
    AndOp, OrOp, AssignOp, LParanthesis, RParanthesis, Comma, Semicolon, LCurly, RCurly,
    // other
    Identifier, Constant, StringLiteral
}

namespace JASON_Compiler
{
    public class Token
    {
        public string lex;
        public Token_Class token_type;
    }

    public class Scanner
    {
        public List<Token> Tokens = new List<Token>();
        Dictionary<string, Token_Class> ReservedWords = new Dictionary<string, Token_Class>();
        Dictionary<string, Token_Class> Operators = new Dictionary<string, Token_Class>();

        public Scanner()
        {
            // reserved words
            ReservedWords.Add("int", Token_Class.Int);
            ReservedWords.Add("float", Token_Class.Float);
            ReservedWords.Add("string", Token_Class.String);
            ReservedWords.Add("read", Token_Class.Read);
            ReservedWords.Add("write", Token_Class.Write);
            ReservedWords.Add("repeat", Token_Class.Repeat);
            ReservedWords.Add("until", Token_Class.Until);
            ReservedWords.Add("if", Token_Class.If);
            ReservedWords.Add("elseif", Token_Class.ElseIf);
            ReservedWords.Add("else", Token_Class.Else);
            ReservedWords.Add("then", Token_Class.Then);
            ReservedWords.Add("return", Token_Class.Return);
            ReservedWords.Add("endl", Token_Class.Endl);
            ReservedWords.Add("end", Token_Class.End);
            ReservedWords.Add("main", Token_Class.Main);
            // operators and punctuation
            Operators.Add("+", Token_Class.PlusOp);
            Operators.Add("-", Token_Class.MinusOp);
            Operators.Add("*", Token_Class.MultiplyOp);
            Operators.Add("/", Token_Class.DivideOp);
            Operators.Add("<", Token_Class.LessThanOp);
            Operators.Add(">", Token_Class.GreaterThanOp);
            Operators.Add("=", Token_Class.EqualOp);
            Operators.Add("<>", Token_Class.NotEqualOp);
            Operators.Add("&&", Token_Class.AndOp);
            Operators.Add("||", Token_Class.OrOp);
            Operators.Add(":=", Token_Class.AssignOp);
            Operators.Add("(", Token_Class.LParanthesis);
            Operators.Add(")", Token_Class.RParanthesis);
            Operators.Add(";", Token_Class.Semicolon);
            Operators.Add(",", Token_Class.Comma);
            Operators.Add("{", Token_Class.LCurly);
            Operators.Add("}", Token_Class.RCurly);

        }

        public void StartScanning(string SourceCode)
        {
            // Normalize Unicode dash/minus characters (and similar punctuation) to ASCII '-'
            // Use a regex on the Unicode Dash_Punctuation category and the Unicode minus sign U+2212
            SourceCode = Regex.Replace(SourceCode ?? string.Empty, "[\\p{Pd}\\u2212]", "-");
            // normalize non-breaking spaces to regular spaces and apply Unicode normalization
            SourceCode = SourceCode.Replace('\u00A0', ' ').Normalize(System.Text.NormalizationForm.FormKC);

            // clear previous tokens for a fresh scan
            Tokens.Clear();

            for (int i = 0; i < SourceCode.Length; i++)
            {
                int j = i;
                char CurrentChar = SourceCode[i];
                string CurrentLexeme = CurrentChar.ToString();

                // skip whitespaces
                if (CurrentChar == ' ' || CurrentChar == '\r' || CurrentChar == '\n' || CurrentChar == '\t')
                    continue;

                // skip comments
                if (CurrentChar == '/' && i + 1 < SourceCode.Length && SourceCode[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < SourceCode.Length && !(SourceCode[i] == '*' && SourceCode[i + 1] == '/')) i++;
                    // if */ was never found
                    if (i + 1 >= SourceCode.Length)
                    {
                        Errors.Error_List.Add("Unterminated comment");
                        break;
                    }
                    // if it was found
                    i++;
                }

                //strings
                else if (CurrentChar == '"')
                {
                    j = i + 1;
                    CurrentLexeme = ""; // remove the opening quote - best to keep the lexeme clean for error reporting
                    while (j < SourceCode.Length && SourceCode[j] != '"')
                    {
                        // handle escaped quotes \"
                        if (SourceCode[j] == '\\' && j + 1 < SourceCode.Length && SourceCode[j + 1] == '"') j++;

                        CurrentLexeme += SourceCode[j].ToString();
                        j++;
                    }

                    // if the closing " was never found, pass the partial lexeme ("....) to FindTokenClass to report the error
                    if (j >= SourceCode.Length)
                    {
                        FindTokenClass(CurrentLexeme);
                        i = j;
                        continue;
                    }

                    // found the closing "
                    //CurrentLexeme += SourceCode[j].ToString();
                    Tokens.Add(new Token { lex = CurrentLexeme, token_type = Token_Class.StringLiteral });
                    i = j;

                }

                //identifiers & keywords
                else if (char.IsLetter(CurrentChar))
                {
                    j = i + 1;
                    while (j < SourceCode.Length && char.IsLetterOrDigit(SourceCode[j]))
                    {
                        CurrentLexeme += SourceCode[j].ToString();
                        j++;
                    }
                    FindTokenClass(CurrentLexeme);
                    i = j - 1;
                }

                //numbers
                else if (char.IsDigit(CurrentChar))
                {
                    j = i + 1;
                    while (j < SourceCode.Length && (char.IsDigit(SourceCode[j]) || SourceCode[j] == '.'))
                    {
                        CurrentLexeme += SourceCode[j].ToString();
                        j++;
                    }
                    FindTokenClass(CurrentLexeme);
                    i = j - 1;
                }

                //operators
                else
                {
                    if (i + 1 < SourceCode.Length)
                    {
                        if (CurrentChar == ':' && SourceCode[i + 1] == '=')
                        {
                            CurrentLexeme = ":=";
                            i++;
                        }
                        else if (CurrentChar == '<' && SourceCode[i + 1] == '>')
                        {
                            CurrentLexeme = "<>";
                            i++;
                        }
                        else if (CurrentChar == '&' && SourceCode[i + 1] == '&')
                        {
                            CurrentLexeme = "&&";
                            i++;
                        }
                        else if (CurrentChar == '|' && SourceCode[i + 1] == '|')
                        {
                            CurrentLexeme = "||";
                            i++;
                        }
                    }
                    FindTokenClass(CurrentLexeme);
                }
            }

            JASON_Compiler.TokenStream = Tokens;
        }
        void FindTokenClass(string Lex)
        {
            Token_Class TC;
            Token Tok = new Token();
            Tok.lex = Lex;

            if (ReservedWords.ContainsKey(Lex))
            {
                TC = ReservedWords[Lex];
                Tok.token_type = TC;
                Tokens.Add(Tok);
            }

            else if (Operators.ContainsKey(Lex))
            {
                TC = Operators[Lex];
                Tok.token_type = TC;
                Tokens.Add(Tok);
            }

            else if (isIdentifier(Lex))
            {
                TC = Token_Class.Identifier;
                Tok.token_type = TC;
                Tokens.Add(Tok);
            }

            else if (isConstant(Lex))
            {
                TC = Token_Class.Constant;
                Tok.token_type = TC;
                Tokens.Add(Tok);
            }

            else
            {
                Errors.Error_List.Add("Unidentified Token " + Lex);
            }


        }



        bool isIdentifier(string lex)
        {
            bool isValid = true;
            if (string.IsNullOrEmpty(lex) || !char.IsLetter(lex[0]))
            { isValid = false; }

            else
            {
                for (int i = 1; i < lex.Length; i++)
                {
                    if (!char.IsLetterOrDigit(lex[i]))
                    {
                        isValid = false;
                    }
                }
            }
            return isValid;
        }
        bool isConstant(string lex)
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(lex)) return false;
            if (lex[0] == '.' || lex[lex.Length - 1] == '.') return false;

            int dotCount = 0;
            for (int i = 0; i < lex.Length; i++)
            {
                if (lex[i] == '.')
                {
                    dotCount++;
                    if (dotCount > 1)
                        isValid = false;
                }
                else if (!char.IsDigit(lex[i]))
                {
                    isValid = false;
                }
            }
            return isValid;
        }
    }
}
