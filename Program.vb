Imports System
Imports System.IO

Module Program
    
    Class part

        Public linePosition as Integer
        Public name as String
        Public lines as new List(of action)
        Public variables as new list(of variable)

        public sub new(byval name as String, byval linePosition as Integer)
            me.name = name
            me.linePosition = linePosition
        end sub

    End Class
    Class action

        Public tokens as new List(of token)
        Public linePosition as Integer
        Public brut as String
        Public parentPart as part

        public sub new(byval tokens as List(of token), byval linePosition as Integer, byval brut as String, byref parentPart as part)
            For Each tok As token In tokens
                me.tokens.add(tok)
            Next
            me.linePosition = linePosition
            me.brut = brut
            me.parentPart = parentPart
        end sub

    End Class

    Class token
        
        Public value as String
        Public type as tokenType

        Public sub new(byval type as tokenType, Optional  byval value as String = "")
            Me.type = type
            Me.value = value
        end sub

    End Class
    Enum tokenType
        int
        float
        str
        text
        
        puts
        gets
        var
        let_
        goto_
        twoPoint
        set_
    End Enum
    Function lineToTokens(byval line as String)
        
        dim tokens as new List(of token)

        while line.Length > 0

            dim currentChar as Char = line(0)
            line = line.Substring(1)
            
            If currentChar = """" Then

                'String     
                dim createString = ""
                while not line(0) = """"
                    createString &= line(0)
                    line = line.Substring(1)
                End While
                line = line.Substring(1)
                tokens.add(new token(tokenType.str, createString))

            ElseIf "0123456789".Contains(currentChar) Then

                dim number as String = currentChar
                dim dotCount as Integer = 0

                while ".0123456789".Contains(line(0))
                    number &= line(0)
                    if line(0) = "." Then
                        dotCount += 1
                    end if
                    line = line.Substring(1)
                end while

                if dotCount = 0 Then
                    tokens.add(new token(tokenType.int, number))
                elseif dotCount = 1 Then
                    tokens.add(new token(tokenType.float, number))
                else
                    Console.WriteLine("Two many dot for a number """ & number & """")
                    Console.WriteLine(vbTab & line)
                    End
                end if
            
            Elseif "abcdefghijklmnopkrstuvwxyzABCDEFGHIJKLMNOPKRSTUVWXYZ_".Contains(currentChar) Then

                dim word as String = currentChar
                while "abcdefghijklmnopkrstuvwxyzABCDEFGHIJKLMNOPKRSTUVWXYZ_".Contains(line(0))
                    word &= line(0)
                    line = line.Substring(1)
                    if not line.Length > 0 then
                        exit while
                    end if
                end while

                word = word.ToLower()
                if word = "puts" then
                    tokens.add(new token(tokenType.puts))
                elseif word = "var" then
                    tokens.add(new token(tokenType.var))
                elseif word = "let" then
                    tokens.add(new token(tokenType.let_))
                elseif word = "set" then
                    tokens.add(new token(tokenType.set_))
                elseif word = "gets" then
                    tokens.add(new token(tokenType.gets))
                elseif word = "goto" then
                    tokens.add(new token(tokenType.goto_))
                else
                    tokens.add(new token(tokenType.text, word))
                end if

            Elseif currentChar = " " or currentChar = vbTab Then
                Continue while

            Elseif currentChar = ":" Then
                tokens.add(new token(tokenType.twoPoint))

            Else
                Console.WriteLine("Unknown character """ & currentChar & """")
                Console.WriteLine(vbTab & line)
                End
            End If
            

        End While   

        return tokens

    End Function

    Class variable
        
        Public name as String
        Public value as String
        Public type as valueType

        Public sub new(byval name as String, Optional type as valueType = valueType.unknown, Optional value as String = "")
            me.name = name
        end sub

    End Class
    Enum valueType
        str
        int
        float
        unknown
    End Enum
    
    'Variables
    Public variables as new List(of variable)
    Public parts as new list(of part)
    
    Sub Main(args As String())
    
        if not args.Count > 0 Then
            'Check argument (and show usage if not correctly use)
            Console.WriteLine("Use : sus [filename]")
           end
        end if

        'Variables
        dim lines as new List(of String)
        dim linesPositions as new list(of Integer)

        Try
            'Load code
            dim sr as new StreamReader(args(0))
            dim lineCounter as Integer = 0
            do until sr.EndOfStream()
                lineCounter += 1
                dim line as String = sr.readLine()
                if line.StartsWith("//") or line = "" Then
                    Continue do
                end if
                lines.Add(line)
                linesPositions.add(lineCounter)
            loop
        sr.Close()
        Catch ex As Exception
            'Cannot load file
            Console.WriteLine("File doesn't exist !")
            end
        End Try
        
        'Tokenise the code
        dim currentPart as new part("main", -1)
        For lineIndex As Integer = 0 to lines.count - 1
            dim line as String = lines(lineIndex)
            if line.EndsWith(":") Then
                parts.add(currentPart)
                dim tokens as list(of token) = lineToTokens(line)
                if (not tokens.count = 2) or (not tokens(0).type = tokenType.text) Then
                    pushError("Part definition should be writen like this : [part_name]:", linesPositions(lineIndex), line)
                end if
                currentPart = new part(tokens(0).value, linesPositions(lineIndex))
            else
                currentPart.lines.add(new action(lineToTokens(line), linesPositions(lineIndex), line, currentPart))
            end if
        Next
        parts.add(currentPart)
        
        'Execute code
        For Each p As part In parts
            if p.name = "main"
                executePart(p)
                End
            end if
        Next
                
    End Sub
   
    Sub executePart(byval p as part)

        For Each line As action In p.lines
            executeLine(line)
        Next
        
    End Sub
    

    Sub executeLine(byval line as action)
    
        dim tokens as list(of token) = line.tokens

        if not tokens.Count > 0 Then
            exit sub
        end if

        Select Case  tokens(0).type

            Case tokenType.puts
                if not tokens.Count > 1 then
                    pushError("Puts usage : puts <message>", line.linePosition, line.brut)
                end if
                Console.WriteLine(getStringValue(tokens(1), line))
            

            Case tokenType.goto_
                if not tokens.Count > 1 then
                    pushError("Goto usage : goto <part_name>", line.linePosition, line.brut)
                end if
                if not tokens(1).type = tokenType.text then
                    pushError("Goto usage : goto <part_name>", line.linePosition, line.brut)
                end if
                dim target as part = Nothing
                For Each p As part In parts
                    if p.name = tokens(1).value then
                        target = p
                        exit for
                    end if
                Next
                if target is Nothing then
                    pushError("Part """ & tokens(1).value & """ is not defined", line.linePosition, line.brut)
                end if
                executePart(target)

            case tokenType.gets
                if not tokens.Count > 1 then
                    pushError("Gets usage : gets <variable_name> [message]", line.linePosition, line.brut)
                end if
                if not tokens(1).type = tokenType.text then
                    pushError("Gets usage : gets <variable_name> [message]", line.linePosition, line.brut)
                end if
                dim var as variable = getVariable(tokens(1).value, line)
                var.type = valueType.str
                if tokens.count > 2 then
                    if not getTokType(tokens(2), line) = valueType.str then
                        pushError("The message need to be a string value", line.linePosition, line.brut)
                    end if
                    Console.Write(getStringValue(tokens(2), line))
                end if
                var.value = Console.ReadLine()                

            Case tokenType.var
                if not tokens.Count > 1 then
                    pushError("Var usage : var <variable name> [value]", line.linePosition, line.brut)
                end if
                if not tokens(1).type = tokenType.text then
                    pushError("Var usage : var <variable name> [value]", line.linePosition, line.brut)
                end if
                dim var as new variable(tokens(1).value, valueType.str)
                if tokens.count > 2 then
                    var.type = getTokType(tokens(2), line)
                    select case var.type
                        case valueType.str
                            var.value = getStringValue(tokens(2), line)
                        case valueType.int
                            var.value = getIntValue(tokens(2), line)
                        case valueType.float
                            var.value = getFloatValue(tokens(2), line)
                        case else
                            pushError("The <" & var.type.ToString() & "> type cannot be obtained", line.linePosition, line.brut)
                    end select
                end if
                variables.add(var)

            Case tokenType.let_
                if not tokens.Count > 1 then
                    pushError("Let usage : let <variable name> [value]", line.linePosition, line.brut)
                end if
                if not tokens(1).type = tokenType.text then
                    pushError("Let usage : let <variable name> [value]", line.linePosition, line.brut)
                end if
                dim var as new variable(tokens(1).value, valueType.str)
                if tokens.count > 2 then
                    var.type = getTokType(tokens(2), line)
                    select case var.type
                        case valueType.str
                            var.value = getStringValue(tokens(2), line)
                        case valueType.int
                            var.value = getIntValue(tokens(2), line)
                        case valueType.float
                            var.value = getFloatValue(tokens(2), line)
                        case else
                            pushError("The <" & var.type.ToString() & "> type cannot be obtained", line.linePosition, line.brut)
                    end select
                end if
                line.parentPart.Variables.add(var)

            case tokenType.set_
                if not tokens.Count > 2 then
                    pushError("Set usage : set <variable name> <new_value>", line.linePosition, line.brut)
                end if
                if not tokens(1).type = tokenType.text then
                    pushError("Set usage : set <variable name> <new_value>", line.linePosition, line.brut)
                end if
                dim var as variable = getVariable(tokens(1).value, line)
                var.type = getTokType(tokens(2), line)
                select case var.type
                    case valueType.str
                        var.value = getStringValue(tokens(2), line)
                    case valueType.int
                        var.value = getIntValue(tokens(2), line)
                    case valueType.float
                        var.value = getFloatValue(tokens(2), line)
                    case else
                        pushError("The <" & var.type.ToString() & "> type cannot be obtained", line.linePosition, line.brut)
                end select
                

            case else
                pushError("Line must start with a actions name", line.linePosition, line.brut)

        End Select
        

    End Sub

    Function getTokType(byval tok as token, byval parentLine as action) As valueType
        
        select case tok.type

            case tokenType.str
                return valueType.str

            case tokenType.int
                return valueType.int

            case tokenType.float
                return valueType.float

            case tokenType.text
                dim var as variable = getVariable(tok.value, parentLine)
                return var.type

            case else
               return valueType.unknown

        end select

    End Function
    

    Function getVariable(byval name as String, byref parentLine as action) As variable
        
        'Const
        For Each var As variable In variables
            if var.name = name then
                return var
            end if
        Next

        'Local
        For Each var As variable In parentLine.parentPart.variables
            if var.name = name then
                return var
            end if
        Next

        'No variable
        pushError("Variable """ & name & """ is not defined", parentLine.linePosition, parentLine.brut)
        Return Nothing
        
    End Function
    
    
    Function getStringValue(byval tok as token, byval parentLine as action) As String
        
        if tok.type = tokenType.str then

            return tok.value

        elseif tok.type = tokenType.text then
            dim var as variable = getVariable(tok.value, parentLine)
            if not var.type = valueType.str then
                pushError("The variable is not a <string>", parentLine.linePosition, parentLine.brut)
            end if
            return var.value
        
        else
            pushError("The <" & tok.type.ToString() & "> token doesn't contain any usable string", parentLine.linePosition, parentLine.brut)
            return ""
        end if

    End Function
    Function getIntValue(byval tok as token, byval parentLine as action) As String
        
        if tok.type = tokenType.int then

            return tok.value

        elseif tok.type = tokenType.text then
            dim var as variable = getVariable(tok.value, parentLine)
            if not var.type = valueType.int then
                pushError("The variable is not a <int>", parentLine.linePosition, parentLine.brut)
            end if
            return var.value
        
        else
            pushError("The <" & tok.type.ToString() & "> token doesn't contain any usable int number", parentLine.linePosition, parentLine.brut)
            return ""
        end if

    End Function
    Function getFloatValue(byval tok as token, byval parentLine as action) As String
        
        if tok.type = tokenType.float then

            return tok.value

        elseif tok.type = tokenType.text then
            dim var as variable = getVariable(tok.value, parentLine)
            if not var.type = valueType.float then
                pushError("The variable is not a <float>", parentLine.linePosition, parentLine.brut)
            end if
            return var.value
        
        else
            pushError("The <" & tok.type.ToString() & "> token doesn't contain any usable float number", parentLine.linePosition, parentLine.brut)
            return ""
        end if

    End Function
    
    Sub pushError(byval message as String, byval linePosition as Integer, Optional byval line as String = "")
        Console.WriteLine("FATAL ERROR: " & message)
        If not line = "" Then
            Console.WriteLine(vbTab & line)
        End If
        Console.WriteLine("Line " & linePosition.ToString())
        End
    End Sub
    

End Module