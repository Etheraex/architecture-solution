use std::env;
use serde::Deserialize;

pub fn get_host_url() -> String {
    env::args()
        .nth(2)
        .unwrap_or_else(|| "localhost:80".to_string())
}

pub fn get_number_of_items_to_post() -> usize {
    env::args()
        .nth(1)
        .and_then(|s| s.parse().ok())
        .unwrap_or(5)
}

pub fn get_token_str(client: &reqwest::blocking::Client, token_url: String) -> String {

    #[derive(Deserialize)]
    struct TokenResponse {
        token: String,
    }

    match client.get(token_url).send() {
        Ok(response) => {
            let token_response: TokenResponse = response.json().unwrap_or_else(|error| {
                panic!("Could not deserialize TokenResponse: {error}");
            });

            token_response.token
        }
        Err (e) => {
            panic!("Error fetching token: {e}");
        }
    }
}