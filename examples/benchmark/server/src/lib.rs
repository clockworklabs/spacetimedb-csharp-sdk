use spacetimedb::{spacetimedb, TableType};

#[spacetimedb(table)]
pub struct User {
    #[primarykey]
    #[autoinc]
    id: u32,
    name: String,
    age: u8,
}

#[spacetimedb(reducer)]
pub fn insert(name: String, age: u8) {
    User::insert(User { id: 0, name, age }).unwrap();
}

#[spacetimedb(reducer)]
pub fn iter_all() {
    for user in User::iter() {
        std::hint::black_box(user);
    }
}

#[spacetimedb(reducer)]
pub fn clear_all() {

    for user in User::iter() {
        user.delete();
    }
}
