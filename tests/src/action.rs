pub enum Action {
    EndToEnd,
    Auth,
    None
}

impl Action {
    pub fn from_args(s: &str) -> Result<Self, String> {
        match s {
            "END_TO_END" => Ok(Action::EndToEnd),
            "AUTH" => Ok(Action::Auth),
            other => Err(format!("Unknown action: {other}"))
        }
    }
}