Imports System
Imports System.IO

Module Program
    
    Class part

        Public linePosition as Integer
        Public name as String
        Public lines as new List(of line)

        public sub new(byval name as String, byval linePosition as Integer)
            me.name = name
            me.linePosition = linePosition
        end sub

    End Class
    Class line

        Public tokens as List(of token)
        Public linePosition as Integer

        public sub new(byval tokens as List(of token), byval linePosition as Integer)
            For Each tok As token In tokens
                me.tokens.add(tok)
            Next
            me.linePosition = linePosition            
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
        setvar
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
        dim tokens as new List(of List(of token))

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
        For lineIndex As Integer = 0 to lines.count
            dim line as String = lines(lineIndex)
            if line.EndWith(":") Then

            else
                tokens.Add(lineToTokens(line))
            end if
        Next
        
        'Execute code
        For Each line As List(of token) in tokens
            executeLine(line)
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
            
            Elseif "abcdefghijklmnopkrstuvwxyzABCDEFGHIJKLMNOPKRSTUVWXYZ".Contains(currentChar) Then

                dim word as String = currentChar
                while "abcdefghijklmnopkrstuvwxyzABCDEFGHIJKLMNOPKRSTUVWXYZ".Contains(line(0))
                    word &= line(0)
                    line = line.Substring(1)
                end while

                word = word.ToLower()
                if word = "puts" then
                    tokens.add(new token(tokenType.puts))
                elseif word = "var" then
                    tokens.add(new token(tokenType.var))
                elseif word = "set" then
                    tokens.add(new token(tokenType.setvar))
                else
                    tokens.add(new token(tokenType.text, word))
                end if

            Elseif currentChar = " " or currentChar = vbTab Then
                Continue while

            Else
                Console.WriteLine("Unknown character """ & currentChar & """")
                Console.WriteLine(vbTab & line)
                End
            End If
            

        End While   

        return tokens

    End Function
    

    Sub executeLine(byval tokens as List(of token))
    
        if not tokens.Count > 0 Then
            exit sub
        end if

        Select Case  tokens(0).type

            Case tokenType.puts
                if not tokens.Count > 1 then
                    'push puts usage error
                end if
                Console.WriteLine(getStringValue(tokens(1)))
            
            Case tokenType.var
                if not tokens.Count > 1 then
                    'push puts usage error
                end if
                

            case else
                pushError("Line must start with a actions name")

        End Select
        

    End Sub
    
    Function getStringValue(byval tok as token) As String
        
        if tok.type = tokenType.str then

            return tok.value

        elseif tok.type = tokenType.text then
            for Each var as variable in variables
                if var.name = tok.value then
                    if var.type = valueType.str Then
                        return var.value
                    else
                        'push type error
                    end if
                end if
            Next
            'Push variable doesn't exist error
        
        else
            'push type error
        
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
