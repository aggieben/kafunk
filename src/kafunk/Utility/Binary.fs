namespace Kafunk

open System
open System.Text

/// Big-endian operations on binary data.
module Binary =

  type Segment = ArraySegment<byte>

  type Reader<'a> = Segment -> 'a * Segment

  type Writer<'a> = 'a -> Segment -> Segment

  let empty : Segment =
    Segment()

  let inline zeros (count : int) : Segment =
    Segment(Array.zeroCreate count)

  /// Sets the size of the segment, leaving the offset the same.
  let inline resize (count : int) (a : Segment) =
    Segment(a.Array, a.Offset, count)

  /// Sets the offset, adjusting count as needed.
  let inline offset (offset : int) (a : Segment) =
    Segment(a.Array, offset, (a.Count - (offset - a.Offset)))

  /// Shifts the offset by the specified amount, adjusting count as needed.
  let inline shiftOffset (d : int) (a : Segment) : Segment =
    offset (a.Offset + d) a

  let inline ofArray (bytes : byte[]) : Segment =
    Segment(bytes, 0, bytes.Length)

  let inline toArray (s : Segment) : byte[] =
    let arr = Array.zeroCreate s.Count
    System.Buffer.BlockCopy(s.Array, s.Offset, arr, 0, s.Count)
    arr

  let inline toString (buf : Segment) : string =
    if isNull buf.Array then null
    else Encoding.UTF8.GetString(buf.Array, buf.Offset, buf.Count)

  let inline copy (src : Segment) (dest : Segment) (count : int) =
    System.Buffer.BlockCopy(src.Array, src.Offset, dest.Array, dest.Offset, count)

  let inline sizeBool (_:bool) = 1

  let inline peekBool (buf: Segment) : bool =
    System.Convert.ToBoolean(buf.Array.[buf.Offset])
  
  let inline readBool (buf: Segment) : bool * Segment =
    (peekBool buf, (buf |> shiftOffset 1))
  
  let inline pokeBool (x : bool) (buf : Segment) =
    buf.Array.[buf.Offset] <- System.Convert.ToByte(x)
    buf

  let inline writeBool (x: bool) (buf: Segment) =
    pokeBool x buf |> shiftOffset 1

  let inline sizeInt8 (_:int8) = 1

  let inline peekInt8Offset (buf : Segment) (offset: int) : int8 =
    int8 buf.Array.[buf.Offset + offset]

  let inline peekInt8 (buf : Segment) : int8 =
    peekInt8Offset buf 0

  let inline readInt8 (buf : Segment) : int8 * Segment =
    (peekInt8 buf, (buf |> shiftOffset 1))

  let inline pokeInt8 (x : int8) (buf : Segment) =
    buf.Array.[buf.Offset] <- byte x
    buf

  let inline writeInt8 (x : int8) (buf : Segment) =
    pokeInt8 x buf |> shiftOffset 1

  let inline writeByte (x:byte) (buf : Segment) =
    buf.Array.[buf.Offset] <- x
    buf |> shiftOffset 1

  let inline sizeInt16 (_:int16) = 2

  let inline peekInt16 (buf : Segment) : int16 =
    let offset = buf.Offset
    let array = buf.Array
    (int16 array.[offset + 0] <<< 8) ||| (int16 array.[offset + 1])

  let inline readInt16 (buf : Segment) : int16 * Segment =
    (peekInt16 buf, buf |> shiftOffset 2)

  let inline pokeInt16In (x : int16) (array:byte[]) (offset:int) =
    array.[offset + 0] <- byte (x >>> 8)
    array.[offset + 1] <- byte x

  let inline pokeInt16 (x : int16) (buf : Segment) =
    pokeInt16In x buf.Array buf.Offset
    buf

  let inline writeInt16 (x : int16) (buf : Segment) =
    pokeInt16 x buf |> shiftOffset 2

  let inline sizeInt32 (_:int32) = 4

  let inline peekInt32 (buf : Segment) : int32 =
    let offset = buf.Offset
    let array = buf.Array
    (int32 array.[offset + 0] <<< 24) |||
    (int32 array.[offset + 1] <<< 16) |||
    (int32 array.[offset + 2] <<< 8) |||
    (int32 array.[offset + 3] <<< 0)

  let inline readInt32 (buf : Segment) : int32 * Segment =
    (peekInt32 buf, (buf |> shiftOffset 4))

  let inline pokeInt32In (x : int32) (array:byte[]) (offset:int) =
    array.[offset + 0] <- byte (x >>> 24)
    array.[offset + 1] <- byte (x >>> 16)
    array.[offset + 2] <- byte (x >>> 8)
    array.[offset + 3] <- byte x
   
  let inline pokeInt32 (x : int32) (buf : Segment) =
    pokeInt32In x buf.Array buf.Offset
    buf

  /// Writes the int64 as a uint32.
  let inline pokeUInt32In (x : int64) (array:byte[]) (offset:int) =
    pokeInt32In (int (x &&& 0xffffffffL)) array offset

  let inline writeInt32 (x : int32) (buf : Segment) =
    pokeInt32 x buf |> shiftOffset 4

  let inline sizeInt64 (_:int64) = 8

  let inline peekInt64 (buf : Segment) : int64 =
    let offset = buf.Offset
    let array = buf.Array
    (int64 array.[offset + 0] <<< 56) |||
    (int64 array.[offset + 1] <<< 48) |||
    (int64 array.[offset + 2] <<< 40) |||
    (int64 array.[offset + 3] <<< 32) |||
    (int64 array.[offset + 4] <<< 24) |||
    (int64 array.[offset + 5] <<< 16) |||
    (int64 array.[offset + 6] <<< 8) |||
    (int64 array.[offset + 7])

  let inline readInt64 (buf : Segment) : int64 * Segment =
    (peekInt64 buf, (buf |> shiftOffset 8))

  let inline pokeInt64 (x : int64) (buf : Segment) =
    let offset = buf.Offset
    let array = buf.Array
    array.[offset + 0] <- byte (x >>> 56)
    array.[offset + 1] <- byte (x >>> 48)
    array.[offset + 2] <- byte (x >>> 40)
    array.[offset + 3] <- byte (x >>> 32)
    array.[offset + 4] <- byte (x >>> 24)
    array.[offset + 5] <- byte (x >>> 16)
    array.[offset + 6] <- byte (x >>> 8)
    array.[offset + 7] <- byte x
    buf

  let inline writeInt64 (x : int64) (buf : Segment) =
    pokeInt64 x buf |> shiftOffset 8

  let sizeVarint (value:int) =
    //if value < 128 then 1 else
    let mutable v = (value <<< 1) ^^^ (value >>> 31)
    let mutable bytes = 1
    while ((v &&& 0xffffff80) <> 0) do
      bytes <- bytes + 1
      v <- v >>> 7
    bytes

  let sizeVarint64 (value:int64) =
    let mutable v = (value <<< 1) ^^^ (value >>> 63)
    let mutable bytes = 1
    while ((v &&& 0xffffffffffffff80L) <> 0L) do
      bytes <- bytes + 1
      v <- v >>> 7
    bytes

  let NULL_SIZE_VARINT = sizeVarint -1

  let inline write2
    (writeA : Writer<'a>)
    (writeB : Writer<'b>)
    ((a, b) : ('a * 'b))
    (buf : Segment)
    : Segment =
    buf |> writeA a |> writeB b

  let inline sizeBytes (bytes:Segment) =
      sizeInt32 bytes.Count + bytes.Count

  let inline sizeBytesVarint (bytes:Segment) =
    if isNull bytes.Array then NULL_SIZE_VARINT
    else sizeVarint bytes.Count + bytes.Count

  let inline writeBytes (bytes:Segment) buf =
    if isNull bytes.Array then
      writeInt32 -1 buf
    else
      let buf = writeInt32 bytes.Count buf
      System.Buffer.BlockCopy(bytes.Array, bytes.Offset, buf.Array, buf.Offset, bytes.Count)
      buf |> shiftOffset bytes.Count

  let inline readBytes (buf:Segment) : Segment * Segment =
    let length, buf = readInt32 buf
    if length = -1 then
      (empty, buf)
    else
      let arr = Segment(buf.Array, buf.Offset, length)
      (arr, buf |> shiftOffset length)

  // TODO: Do we need to support non-ascii values here? This currently
  // assumes each character is always encoded in UTF-8 by a single byte.
  // We should also error out for strings wich are too long for the
  // int16 to represent.
  let inline sizeString (str:string) =
    if isNull str then sizeInt16 0s
    else sizeInt16 (int16 str.Length) + str.Length

  let inline writeString (str : string) (buf : Segment) =
    if isNull str then
      writeInt16 -1s buf
    else
      let buf = writeInt16 (int16 str.Length) buf
      let read = Encoding.UTF8.GetBytes(str, 0, str.Length, buf.Array, buf.Offset)
      buf |> shiftOffset read

  let inline readString (buf : Segment) : string * Segment =
    let length, buf = readInt16 buf
    let length = int length
    if length = -1 then
      (null, buf)
    else
      let str = Encoding.UTF8.GetString (buf.Array, buf.Offset, length)
      (str, buf |> shiftOffset length)

  let inline sizeArray (a : 'a []) (size : 'a -> int) =
    sizeInt32 a.Length + (a |> Array.sumBy size)

  let inline writeArray (arr : 'a[]) (write : Writer<'a>) (buf : Segment) : Segment =
    if isNull arr then
      writeInt32 -1 buf
    else
      let mutable buf = writeInt32 arr.Length buf
      for i = 0 to arr.Length - 1 do
        buf <- write arr.[i] buf
      buf

  let readArray (read : Reader<'a>) (buf : Segment) : 'a[] * Segment =
    let n, buf = readInt32 buf
    let mutable buf = buf
    let arr = Array.zeroCreate n
    for i = 0 to n - 1 do
      let elem, buf' = read buf
      arr.[i] <- elem
      buf <- buf'
    (arr, buf)

type BinaryZipper (buf:ArraySegment<byte>) =
  
  let mutable buf = buf

  member __.Buffer 
    with get () = buf 
    and set b = buf <- b

  member __.ShiftOffset (n) =
    buf <- Binary.shiftOffset n buf

  member __.ReadBool () : bool =
    let r = Binary.peekBool buf
    r
  
  member __.WriteBool (x:bool) =
    buf <- Binary.writeBool x buf

  member __.TryPeekIn8AtOffset (offset:int) : int8 =
    if buf.Count > buf.Offset + offset then Binary.peekInt8Offset buf offset
    else 0y

  member __.ReadInt8 () : int8 =
    let r = Binary.peekInt8 buf
    __.ShiftOffset 1
    r
  
  member __.WriteInt8 (x:int8) =
    buf <- Binary.writeInt8 x buf

  member __.WriteByte (x:byte) =
    buf <- Binary.writeByte x buf

  member __.PeekIn16 () = 
    Binary.peekInt16 buf

  member __.ReadInt16 () : int16 =
    let r = Binary.peekInt16 buf
    __.ShiftOffset 2
    r

  member __.WriteInt16 (x:int16) =
    buf <- Binary.writeInt16 x buf

  member __.PeekIn32 () : int32 = Binary.peekInt32 buf

  member __.ReadInt32 () : int32 =
    let r = Binary.peekInt32 buf
    __.ShiftOffset 4
    r

  member __.WriteInt32 (x:int32) =
    buf <- Binary.writeInt32 x buf

  member __.PeekIn64 () : int64 = Binary.peekInt64 buf

  member __.ReadInt64 () : int64 =
    let r = Binary.peekInt64 buf
    buf <- Binary.shiftOffset 8 buf
    r

  member __.WriteInt64 (x:int64) =
    buf <- Binary.writeInt64 x buf

  member __.ReadVarint () : int =
    let rec go value i =
      let b = int <| __.ReadInt8 ()
      if (b &&& 0x80 <> 0) then
        let value = value ||| ((b &&& 0x7f) <<< i)
        let i = i + 7
        if (i > 28) then failwith "invalid varint" 
        else go value i
      else
        value ||| (b <<< i)
    let value = go 0 0
    (value >>> 1) ^^^ -(value &&& 1)

  member __.WriteVarint (value:int) =
    let mutable v = (value <<< 1) ^^^ (value >>> 31)
    while ((v &&& 0xffffff80) <> 0) do
      let b = byte ((v &&& 0x7f) ||| 0x80)
      __.WriteByte b
      v <- v >>> 7
    __.WriteByte (byte v)
    
  member __.WriteVarint64 (value:int64) =
    let mutable v = (value <<< 1) ^^^ (value >>> 63)
    while ((v &&& 0xffffffffffffff80L) <> 0L) do
      let b = byte ((v &&& 0x7fL) ||| 0x80L)
      __.WriteByte b
      v <- v >>> 7
    __.WriteByte (byte v)

  member __.ReadVarint64 () : int64 =
    let rec go value i =
      let b = int <| __.ReadInt8 ()
      if (b &&& 0x80 <> 0) then
        let value = value ||| int64 ((b &&& 0x7f) <<< i)
        let i = i + 7
        if (i > 63) then failwith "invalid varint64" 
        else go value i
      else
        value ||| int64 (b <<< i)
    let value = go 0L 0
    (value >>> 1) ^^^ -(value &&& 1L)

  member __.WriteBytesVarint (bytes:ArraySegment<byte>) =
    if isNull bytes.Array then
      __.WriteVarint -1
    else
      __.WriteVarint bytes.Count
      System.Buffer.BlockCopy(bytes.Array, bytes.Offset, buf.Array, buf.Offset, bytes.Count)
      __.ShiftOffset bytes.Count

  member __.WriteBytes (bytes:ArraySegment<byte>) =
    if isNull bytes.Array then
      __.WriteInt32 -1
    else
      __.WriteInt32 bytes.Count
      System.Buffer.BlockCopy(bytes.Array, bytes.Offset, buf.Array, buf.Offset, bytes.Count)
      __.ShiftOffset bytes.Count

  member __.WriteBytesNoLengthPrefix (bytes:ArraySegment<byte>) =
    System.Buffer.BlockCopy(bytes.Array, bytes.Offset, buf.Array, buf.Offset, bytes.Count)
    __.ShiftOffset bytes.Count

  member __.ReadBytes () : ArraySegment<byte> =
    let length = __.ReadInt32 ()
    if length = -1 then
      //Binary.empty
      ArraySegment<byte>(buf.Array, buf.Offset, 0)
    else
      let arr = ArraySegment<byte>(buf.Array, buf.Offset, length)
      __.ShiftOffset length
      arr

  member __.ReadVarintBytes () : ArraySegment<byte> =
    let length = __.ReadVarint ()
    if length = -1 then
      ArraySegment<byte>(buf.Array, buf.Offset, 0)
    else
      let arr = ArraySegment<byte>(buf.Array, buf.Offset, length)
      __.ShiftOffset length
      arr

  member __.ReadArrayByteSize (expectedSize:int, read:int -> 'a option) =
    let mutable consumed = 0
    let arr = ResizeArray<_>()
    while consumed < expectedSize && buf.Count > 0 do
      let o' = buf.Offset
      match read consumed with
      | Some a -> arr.Add a
      | _ -> ()
      consumed <- consumed + (buf.Offset - o')
    arr.ToArray()

  member __.ReadVarintString () : string =
    let length = __.ReadVarint ()
    if length = -1 then
      null
    else
      let str = Encoding.UTF8.GetString (buf.Array, buf.Offset, length)
      __.ShiftOffset length
      str

  member __.ReadString () : string =
    let length = __.ReadInt16 ()
    let length = int length
    if length = -1 then
      null
    else
      let str = Encoding.UTF8.GetString (buf.Array, buf.Offset, length)
      __.ShiftOffset length
      str

  member __.ReadArray (read:BinaryZipper -> 'a) : 'a[] =
    let n = __.ReadInt32 ()
    //if n = -1 then [||] else
    let arr = Array.zeroCreate n
    for i = 0 to n - 1 do
      arr.[i] <- read __
    arr

  member __.WriteArray (arr:'a[], write:BinaryZipper * 'a -> unit) =
    if isNull arr then
      __.WriteInt32 (-1)
    else
      __.WriteInt32 (arr.Length)
      for i = 0 to arr.Length - 1 do
        write (__,arr.[i])

  member __.WriteString (s:string) =
    if isNull s then
      __.WriteInt16 -1s
    else
      __.WriteInt16 (int16 s.Length)
      let read = Encoding.UTF8.GetBytes(s, 0, s.Length, buf.Array, buf.Offset)
      __.ShiftOffset read

  /// Creates a BinaryZipper and limits the size to the specified value.
  /// NB: the child BinaryZipper is not linked and the parent must be shifted after the child is consumed.
  member __.Limit (count:int) =
    let buf = __.Slice count
    BinaryZipper(buf)

  member __.Slice (count:int) =
    Binary.Segment(__.Buffer.Array, __.Buffer.Offset, count)

