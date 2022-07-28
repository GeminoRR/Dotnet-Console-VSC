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
        var
        go_to
        setvar
        twoPoint
    End Enum

    Class variable
        
        Public name as String
        Public value as String
        Public type as valueType

        Public sub new(byval name as String, Optional type as valueType = valueType.int, Optional value as String = "")
            me.name = name
        end sub

    End Class
    Enum valueType
        str
        int
        float
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
                elseif word = "set" then
                    tokens.add(new token(tokenType.setvar))
                elseif word = "goto" then
                    tokens.add(new token(tokenType.go_to))
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
                    pushError("Puts usage : puts [message]", line.linePosition, line.brut)
                end if
                Console.WriteLine(getStringValue(tokens(1), line))
            
            Case tokenType.go_to
                if not tokens.Count > 1 then
                    pushError("Goto usage : goto [part_name]", line.linePosition, line.brut)
                end if
                if not tokens(1).type = tokenType.text then
                    pushError("Goto usage : goto [part_name]", line.linePosition, line.brut)
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
                

            Case tokenType.var
                if not tokens.Count > 1 then
                    'push puts usage error
                end if
                

            case else
                pushError("Line must start with a actions name", line.linePosition, line.brut)

        End Select
        

    End Sub

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
        pushError("Variable """ & name & """ if not defined", parentLine.linePosition, parentLine.brut)
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
    
    Sub pushError(byval message as String, byval linePosition as Integer, Optional byval line as String = "")
        Console.WriteLine("FATAL ERROR: " & message)
        If not line = "" Then
            Console.WriteLine(vbTab & line)
        End If
        Console.WriteLine("Line " & linePosition.ToString())
        End
    End Sub
    

End Module