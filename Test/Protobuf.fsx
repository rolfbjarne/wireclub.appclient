
#r "../bin/debug/protobuf-net.dll"  

open System.IO
open ProtoBuf

[<CLIMutable; ProtoContract>]
type Funky = {
    [<ProtoMember(1)>]
    mutable Foo: int
}

[<CLIMutable; ProtoContract>]
type User = {
    [<ProtoMember(1)>]
    mutable Name: string

    [<ProtoMember(2)>]
    mutable Slug: string

    [<ProtoMember(3)>]
    mutable Funky: Funky
}

let user = {
    Name = "Braden"
    Slug = "braden"
    Funky = 
        {
            Foo = 999999
        }
}

let stream = new MemoryStream()
Serializer.Serialize(stream, user)

stream.Seek(0L, SeekOrigin.Begin)
stream.ToArray().Length

let userXX = Serializer.Deserialize<User>(stream)

