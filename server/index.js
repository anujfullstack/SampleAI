import express from 'express'
import cors from 'cors'
import sqlite3 from 'sqlite3'
import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

const app = express()
const PORT = 3001

// Middleware
app.use(cors())
app.use(express.json())

// Sample database setup
const initializeSampleDatabase = () => {
  const dbPath = path.join(__dirname, 'project_123.db')
  
  if (!fs.existsSync(dbPath)) {
    const db = new sqlite3.Database(dbPath)
    
    // Create tables
    db.serialize(() => {
      // Groups table
      db.run(`CREATE TABLE Groups (
        Id INTEGER PRIMARY KEY,
        Name TEXT NOT NULL
      )`)
      
      // Participants table
      db.run(`CREATE TABLE Participants (
        Id INTEGER PRIMARY KEY,
        Name TEXT NOT NULL,
        Email TEXT UNIQUE NOT NULL,
        GroupId INTEGER,
        FOREIGN KEY (GroupId) REFERENCES Groups(Id)
      )`)
      
      // Interests table
      db.run(`CREATE TABLE Interests (
        Id INTEGER PRIMARY KEY,
        Title TEXT NOT NULL
      )`)
      
      // EventInterests table - Fixed structure
      db.run(`CREATE TABLE EventInterests (
        Id INTEGER PRIMARY KEY,
        ParticipantId INTEGER,
        InterestId INTEGER,
        EventName TEXT NOT NULL,
        FOREIGN KEY (ParticipantId) REFERENCES Participants(Id),
        FOREIGN KEY (InterestId) REFERENCES Interests(Id)
      )`)
      
      // Insert sample data
      db.run(`INSERT INTO Groups (Id, Name) VALUES 
        (1, 'Marketing'),
        (2, 'Engineering'),
        (3, 'Sales')`)
      
      db.run(`INSERT INTO Participants (Id, Name, Email, GroupId) VALUES 
        (1, 'John Doe', 'john@example.com', 1),
        (2, 'Jane Smith', 'jane@example.com', 2),
        (3, 'Bob Johnson', 'bob@example.com', 1),
        (4, 'Alice Brown', 'alice@example.com', 3),
        (5, 'Charlie Wilson', 'charlie@example.com', 2)`)
      
      db.run(`INSERT INTO Interests (Id, Title) VALUES 
        (1, 'Artificial Intelligence'),
        (2, 'Machine Learning'),
        (3, 'Data Science'),
        (4, 'Cloud Computing'),
        (5, 'Cybersecurity')`)
      
      // Fixed EventInterests data with proper structure
      db.run(`INSERT INTO EventInterests (ParticipantId, InterestId, EventName) VALUES 
        (1, 1, 'AI Conference 2024'),
        (1, 2, 'ML Workshop'),
        (2, 1, 'AI Conference 2024'),
        (2, 3, 'Data Science Summit'),
        (3, 4, 'Cloud Expo'),
        (4, 1, 'AI Conference 2024'),
        (4, 5, 'Security Conference'),
        (5, 2, 'ML Workshop'),
        (5, 3, 'Data Science Summit')`)
    })
    
    db.close()
    console.log('Sample database created')
  }
}

// Initialize sample database
initializeSampleDatabase()

/**
 * Extract database schema from SQLite database
 */
const extractSchema = (dbPath) => {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath)
    
    db.all(`SELECT name FROM sqlite_master WHERE type='table'`, (err, tables) => {
      if (err) {
        reject(err)
        return
      }
      
      const schema = {}
      let pending = tables.length
      
      if (pending === 0) {
        resolve(schema)
        return
      }
      
      tables.forEach(table => {
        db.all(`PRAGMA table_info(${table.name})`, (err, columns) => {
          if (err) {
            reject(err)
            return
          }
          
          schema[table.name] = columns.map(col => ({
            name: col.name,
            type: col.type,
            notNull: col.notnull === 1,
            primaryKey: col.pk === 1
          }))
          
          pending--
          if (pending === 0) {
            db.close()
            resolve(schema)
          }
        })
      })
    })
  })
}

/**
 * Generate SQL query using OpenAI (simulated)
 */
const generateSQLQuery = async (schema, userQuery) => {
  // In a real implementation, this would call Azure OpenAI
  // For demo purposes, we'll simulate with predefined responses
  
  const schemaText = Object.entries(schema)
    .map(([tableName, columns]) => {
      const columnsText = columns.map(col => `${col.name} (${col.type})`).join(', ')
      return `${tableName}: ${columnsText}`
    })
    .join('\n')
  
  console.log('Schema:', schemaText)
  console.log('User Query:', userQuery)
  
  // Simulate AI response based on query keywords
  const queryLower = userQuery.toLowerCase()
  
  if (queryLower.includes('participants') && queryLower.includes('ai')) {
    return `SELECT DISTINCT p.Name, p.Email, ei.EventName, i.Title as Interest
FROM Participants p
JOIN EventInterests ei ON p.Id = ei.ParticipantId
JOIN Interests i ON ei.InterestId = i.Id
WHERE i.Title LIKE '%Artificial Intelligence%' OR i.Title LIKE '%AI%'`
  } else if (queryLower.includes('marketing') && queryLower.includes('group')) {
    return `SELECT p.Name, p.Email, g.Name as GroupName
FROM Participants p
JOIN Groups g ON p.GroupId = g.Id
WHERE g.Name = 'Marketing'`
  } else if (queryLower.includes('popular') && queryLower.includes('interests')) {
    return `SELECT i.Title, COUNT(ei.ParticipantId) as ParticipantCount
FROM Interests i
JOIN EventInterests ei ON i.Id = ei.InterestId
GROUP BY i.Id, i.Title
ORDER BY ParticipantCount DESC`
  } else if (queryLower.includes('events') && queryLower.includes('participants')) {
    return `SELECT ei.EventName, COUNT(DISTINCT ei.ParticipantId) as ParticipantCount
FROM EventInterests ei
GROUP BY ei.EventName
HAVING COUNT(DISTINCT ei.ParticipantId) > 1
ORDER BY ParticipantCount DESC`
  } else {
    // Default query
    return `SELECT * FROM Participants LIMIT 10`
  }
}

/**
 * Execute SQL query on SQLite database
 */
const executeQuery = (dbPath, sql) => {
  return new Promise((resolve, reject) => {
    const db = new sqlite3.Database(dbPath)
    
    db.all(sql, (err, rows) => {
      if (err) {
        console.error('SQL Error:', err.message)
        console.error('SQL Query:', sql)
        reject(err)
        return
      }
      
      db.close()
      resolve(rows)
    })
  })
}

// API endpoint
app.post('https://localhost:56875/api/Query/ask', async (req, res) => {
  try {
    const { projectId, query } = req.body
    
    if (!projectId || !query) {
      return res.status(400).json({ error: 'projectId and query are required' })
    }
    
    // In a real implementation, this would download from Azure File Share
    const dbPath = path.join(__dirname, `project_${projectId}.db`)
    
    if (!fs.existsSync(dbPath)) {
      return res.status(404).json({ error: `Database for project ${projectId} not found` })
    }
    
    // Extract schema
    const schema = await extractSchema(dbPath)
    
    // Generate SQL query
    const sql = await generateSQLQuery(schema, query)
    console.log('Generated SQL:', sql)
    
    // Execute query
    const data = await executeQuery(dbPath, sql)
    
    // Simulate token usage
    const tokenUsage = {
      prompt: Math.floor(Math.random() * 100) + 50,
      completion: Math.floor(Math.random() * 50) + 20,
      total: 0
    }
    tokenUsage.total = tokenUsage.prompt + tokenUsage.completion
    
    res.json({
      data,
      sql,
      tokenUsage,
      schema: Object.keys(schema)
    })
    
  } catch (error) {
    console.error('Error processing query:', error)
    res.status(500).json({ error: 'Internal server error: ' + error.message })
  }
})

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ status: 'OK', timestamp: new Date().toISOString() })
})

app.listen(PORT, () => {
  console.log(`Server running on http://localhost:${PORT}`)
})