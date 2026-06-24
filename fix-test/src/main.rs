use chrono::Utc;
use rand::Rng;
use serde_json::json;
use std::env;
use std::thread;
use std::time::Duration;
use serde::Deserialize;

#[derive(Deserialize)]
struct TokenResponse {
    token: String,
}

const TICKERS: &[&str] = &[
    "AAPL", "MSFT", "AMZN", "GOOGL", "META", "NVDA", "TSLA", "NFLX", "JPM", "V",
];

// 1 = Buy
// 2 = Sell
const SIDES: &[&str] = &["1", "2"];

// Example: 8=FIX.4.2|9=113|35=D|49=CLIENT|56=BROKER|34=78|52=20260613-08:58:18.274|11=ORD00078|21=1|55=AMZN|54=2|38=500|40=2|44=331.01|59=0|10=224|
fn build_fix(seq: usize, symbol: &str, side: &str, qty: u32, price: f64) -> String {
    let ts = Utc::now().format("%Y%m%d-%H:%M:%S%.3f").to_string();

    let body = format!(
        "35=D|49=CLIENT|56=BROKER|34={seq}|52={ts}|11=ORD{seq:05}|21=1|55={symbol}|54={side}|38={qty}|40=2|44={price:.2}|59=0|"
    );
    let prefix = format!("8=FIX.4.2|9={}|{body}", body.len());

    let checksum: u32 = prefix.bytes().map(u32::from).sum::<u32>() % 256;

    format!("{prefix}10={checksum:03}|")
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let host = env::args()
        .nth(1)
        .unwrap_or_else(|| "localhost:8080".to_string());
    let count = env::args().nth(2).and_then(|s| s.parse().ok()).unwrap_or(5);

    let token_url = format!("http://{host}/token");

    let fix_url = format!("http://{host}/fix");

    println!("Firing {count} FIX messages at {fix_url} (1s interval)...\n");

    let client = reqwest::blocking::Client::new();

    let token_response: TokenResponse = client.get(token_url).send()?.json()?;
    let token = token_response.token;

    println!("Token successfully received: {token}");

    let mut rng = rand::rng();

    for seq in 1..=count {
        let symbol = TICKERS[rng.random_range(0..TICKERS.len())];
        let side = SIDES[rng.random_range(0..SIDES.len())];

        let qty: u32 = rng.random_range(1..=10u32) * 100;
        let price: f64 = rng.random_range(50.0..500.0);

        let msg = build_fix(seq, symbol, side, qty, price);

        match client.post(&fix_url)
                .bearer_auth(&token)
                .json(&json!({"message": msg}))
                .send() {
            Ok(response) => {
                let status = response.status();
                let body = response.text().unwrap_or_default();
                println!(
                    "[{seq}/{count}] {symbol} side={side} -> HTTP {} {body}",
                    status.as_u16()
                );
            }
            Err(e) => {
                eprintln!("[{seq}/{count}] {symbol} -> request failed: {e}");
            }
        }

        if seq < count {
            thread::sleep(Duration::from_secs(1));
        }
    }

    println!("Done.");

    Ok(())
}
