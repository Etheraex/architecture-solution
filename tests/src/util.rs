use std::env;
use serde::Deserialize;

pub fn get_number_of_items_to_post() -> usize {
    env::args()
        .nth(2)
        .and_then(|s| s.parse().ok())
        .unwrap_or(5)
}

pub fn get_token_string(client: &reqwest::blocking::Client, token_url: String)
        -> Result<String, Box<dyn std::error::Error>> {

    #[derive(Deserialize)]
    struct TokenResponse {
        token: String,
    }

    let response = client.get(token_url).send()?;
    let token_response: TokenResponse = response.json()?;

    Ok(token_response.token)
}