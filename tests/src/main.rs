use serde_json::json;
use std::{env, thread};
use std::time::Duration;

mod util;
mod fix_generator;

mod action;
use action::Action;

const HOST: &str = "http://localhost:80";

const TOKEN_URL: &str = "/token";
const VERIFY_TOKEN_URL: &str = "/verify";
const FIX_URL: &str = "/fix";

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let action = match env::args().nth(1) {
        Some(arg) => Action::from_args(&arg)?,
        None => Action::None
    };

    match action {
        Action::EndToEnd => run_end_to_end(),
        Action::Auth => run_auth(),
        Action::None => Ok(()),
    }
}

fn run_end_to_end() -> Result<(), Box<dyn std::error::Error>> {
    let count = util::get_number_of_items_to_post();

    let client = reqwest::blocking::Client::new();

    let token = util::get_token_string(&client, format!("{HOST}{TOKEN_URL}"))?;
    println!("Token successfully received: {token}");

    println!("Firing {count} FIX messages at {FIX_URL} (1s interval)...\n");

    for seq in 1..=count {
        let msg = fix_generator::compose_fix(seq);

        match client.post(format!("{HOST}{FIX_URL}"))
                .bearer_auth(&token)
                .json(&json!({"message": msg}))
                .send() {
            Ok(response) => {
                let status = response.status();
                let body = response.text().unwrap_or_default();
                println!(
                    "[{seq}/{count}] -> HTTP {} {body}",
                    status.as_u16()
                );
            }
            Err(e) => {
                eprintln!("[{seq}/{count}] -> request failed: {e}");
            }
        }
    
        if seq < count {
            thread::sleep(Duration::from_secs(1));
        }
    }

    println!("Done.");

    Ok(())
}

fn run_auth() -> Result<(), Box<dyn std::error::Error>> {
    let client = reqwest::blocking::Client::new();

    test_good_token(&client)?;

    Ok(())
}

fn test_good_token(client: &reqwest::blocking::Client) -> Result<(), Box<dyn std::error::Error>> {
    let token = util::get_token_string(client, format!("{HOST}{TOKEN_URL}"))?;

    match client.get(format!("{HOST}{VERIFY_TOKEN_URL}"))
            .bearer_auth(token)
            .send() {
        Ok(resp) if resp.status().is_success() => println!("Token verified!"),
        Ok(resp) => println!("Bad token: HTTP {}", resp.status()),
        Err(e) => println!("Bad token: {e}"),
    }

    Ok(())
}