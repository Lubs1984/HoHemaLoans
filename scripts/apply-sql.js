#!/usr/bin/env node

// Simple script to execute SQL on Railway database
const { readFileSync } = require('fs');
const { Client } = require('pg');

const connectionString = process.argv[2] || process.env.DATABASE_URL;

if (!connectionString) {
  console.error('❌ ERROR: DATABASE_URL not provided');
  console.error('Usage: node apply-sql.js <connection-string> <sql-file>');
  process.exit(1);
}

const sqlFile = process.argv[3] || 'scripts/add-user-documents-table.sql';
const sql = readFileSync(sqlFile, 'utf8');

async function executeSql() {
  const client = new Client({ connectionString });
  
  try {
    console.log('🚀 Connecting to database...');
    await client.connect();
    console.log('✅ Connected!');
    
    console.log(`📋 Executing: ${sqlFile}`);
    console.log('');
    
    await client.query(sql);
    
    console.log('');
    console.log('✅ SQL executed successfully!');
  } catch (err) {
    console.error('❌ Error:', err.message);
    process.exit(1);
  } finally {
    await client.end();
  }
}

executeSql();
