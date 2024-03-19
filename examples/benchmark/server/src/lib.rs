use spacetimedb::{spacetimedb, TableType};

#[spacetimedb(table)]
pub struct Message {
    #[primarykey]
    #[autoinc]
    id: u32,
    text: String,
}

#[spacetimedb(reducer)]
pub fn noop(_text: String) {}

#[spacetimedb(reducer)]
pub fn insert_all(count: u32) {
    for i in 0..count {
        // pad to ensure strings of reasonable length of 100 bytes for benchmarking
        Message::insert(Message {
            id: 0,
            text: format!("Test_Msg{i:_>92}"),
        })
        .unwrap();
    }
}

#[spacetimedb(reducer)]
pub fn clear_all() {
    for msg in Message::iter() {
        msg.delete();
    }
}
