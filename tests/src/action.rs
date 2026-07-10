pub enum Action {
    EndToEnd,
    Auth,
    RestApi,
    None
}

impl Action {
    pub fn from_args(s: &str) -> Result<Self, String> {
        match s {
            "END_TO_END" => Ok(Action::EndToEnd),
            "AUTH" => Ok(Action::Auth),
            "REST_API" => Ok(Action::RestApi),
            other => Err(format!("Unknown action: {other}"))
        }
    }
}