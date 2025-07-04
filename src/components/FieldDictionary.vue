<script setup lang="ts">
import { ref, computed } from 'vue'

interface Field {
  name: string
  type: string
  description: string
  examples: string[]
  table: string
}

const searchTerm = ref('')
const selectedTable = ref('all')
const showDictionary = ref(false)

// Participant table fields based on your API response
const participantFields: Field[] = [
  {
    name: 'Id',
    type: 'INTEGER',
    description: 'Unique identifier for each participant',
    examples: ['218002', '217671', '216803'],
    table: 'Participant'
  },
  {
    name: 'FirstName',
    type: 'TEXT',
    description: 'First name of the participant',
    examples: ['Anuj', 'John', 'Alice'],
    table: 'Participant'
  },
  {
    name: 'LastName',
    type: 'TEXT',
    description: 'Last name of the participant',
    examples: ['Gupta', 'Smith', 'Johnson'],
    table: 'Participant'
  },
  {
    name: 'Email',
    type: 'TEXT',
    description: 'Email address of the participant',
    examples: ['anuj@ionr.com', 'user@gmail.com', 'test@outlook.com'],
    table: 'Participant'
  },
  {
    name: 'Phone',
    type: 'TEXT',
    description: 'Phone number of the participant',
    examples: ['9915082018', '+1234567890', '555-0123'],
    table: 'Participant'
  },
  {
    name: 'ApplicationId',
    type: 'INTEGER',
    description: 'ID of the application this participant belongs to',
    examples: ['1', '2', '3'],
    table: 'Participant'
  },
  {
    name: 'ApplicationInstanceId',
    type: 'INTEGER',
    description: 'Instance ID of the application',
    examples: ['1', '2', '3'],
    table: 'Participant'
  },
  {
    name: 'IsDeleted',
    type: 'BOOLEAN',
    description: 'Whether the participant is deleted (0 = active, 1 = deleted)',
    examples: ['0', '1', 'false', 'true'],
    table: 'Participant'
  },
  {
    name: 'LinkedInProfile',
    type: 'TEXT',
    description: 'LinkedIn profile URL of the participant',
    examples: ['https://linkedin.com/in/username', 'linkedin.com/in/johndoe'],
    table: 'Participant'
  },
  {
    name: 'CreatedDate',
    type: 'DATETIME',
    description: 'When the participant was created',
    examples: ['2024-01-15', '2024-06-01', '2024-09-01'],
    table: 'Participant'
  },
  {
    name: 'JoinDate',
    type: 'DATETIME',
    description: 'When the participant joined the company',
    examples: ['2024-01-01', '2024-05-15', '2024-08-30'],
    table: 'Participant'
  }
]

const tables = computed(() => {
  const uniqueTables = [...new Set(participantFields.map(f => f.table))]
  return ['all', ...uniqueTables]
})

const filteredFields = computed(() => {
  let filtered = participantFields

  // Filter by table
  if (selectedTable.value !== 'all') {
    filtered = filtered.filter(f => f.table === selectedTable.value)
  }

  // Filter by search term
  if (searchTerm.value.trim()) {
    const search = searchTerm.value.toLowerCase()
    filtered = filtered.filter(f => 
      f.name.toLowerCase().includes(search) ||
      f.description.toLowerCase().includes(search) ||
      f.type.toLowerCase().includes(search)
    )
  }

  return filtered
})

const getTypeColor = (type: string) => {
  switch (type.toUpperCase()) {
    case 'INTEGER': return 'bg-blue-100 text-blue-800'
    case 'TEXT': return 'bg-green-100 text-green-800'
    case 'BOOLEAN': return 'bg-purple-100 text-purple-800'
    case 'DATETIME': return 'bg-orange-100 text-orange-800'
    default: return 'bg-gray-100 text-gray-800'
  }
}

const copyFieldName = async (fieldName: string) => {
  try {
    await navigator.clipboard.writeText(fieldName)
    console.log(`Copied: ${fieldName}`)
  } catch (err) {
    console.error('Failed to copy field name:', err)
  }
}
</script>

<template>
  <div class="card">
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-lg font-semibold text-slate-800 flex items-center">
        <svg class="w-5 h-5 mr-2 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
        </svg>
        Field Dictionary
      </h3>
      <button
        @click="showDictionary = !showDictionary"
        class="text-blue-500 hover:text-blue-600 transition-colors"
      >
        <svg 
          class="w-5 h-5 transform transition-transform"
          :class="{ 'rotate-180': showDictionary }"
          fill="none" 
          stroke="currentColor" 
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
        </svg>
      </button>
    </div>

    <!-- Quick Info -->
    <div class="p-3 bg-blue-50 border border-blue-200 rounded-lg mb-4">
      <p class="text-sm text-blue-800">
        <strong>💡 NQL is for Participants only</strong><br>
        Use these fields when asking questions about participant data.
      </p>
    </div>

    <Transition name="slide">
      <div v-if="showDictionary" class="space-y-4">
        <!-- Search and Filter -->
        <div class="space-y-3">
          <div class="relative">
            <input
              v-model="searchTerm"
              type="text"
              placeholder="Search fields..."
              class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none pl-8"
            />
            <svg class="w-4 h-4 text-slate-400 absolute left-2.5 top-2.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
          </div>

          <div class="flex flex-wrap gap-2">
            <button
              v-for="table in tables"
              :key="table"
              @click="selectedTable = table"
              class="text-xs px-3 py-1 rounded-lg transition-all capitalize"
              :class="selectedTable === table 
                ? 'bg-blue-500 text-white' 
                : 'bg-slate-100 text-slate-700 hover:bg-blue-100'"
            >
              {{ table === 'all' ? 'All Tables' : table }}
            </button>
          </div>
        </div>

        <!-- Fields List -->
        <div class="space-y-3 max-h-96 overflow-y-auto">
          <div v-if="filteredFields.length === 0" class="text-center py-8 text-slate-500">
            <svg class="w-12 h-12 mx-auto mb-3 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
            </svg>
            <p class="text-sm">No fields found</p>
            <p class="text-xs text-slate-400 mt-1">Try a different search term</p>
          </div>

          <div
            v-for="field in filteredFields"
            :key="`${field.table}.${field.name}`"
            class="group p-4 bg-slate-50 hover:bg-blue-50 rounded-lg border border-slate-200 hover:border-blue-300 transition-all duration-200"
          >
            <!-- Field Header -->
            <div class="flex items-start justify-between mb-2">
              <div class="flex items-center gap-3">
                <button
                  @click="copyFieldName(field.name)"
                  class="font-mono text-sm font-semibold text-slate-800 hover:text-blue-600 transition-colors cursor-pointer"
                  :title="`Click to copy: ${field.name}`"
                >
                  {{ field.name }}
                </button>
                <span 
                  class="text-xs px-2 py-1 rounded-full font-medium"
                  :class="getTypeColor(field.type)"
                >
                  {{ field.type }}
                </span>
              </div>
              <span class="text-xs text-slate-500 bg-slate-200 px-2 py-1 rounded">
                {{ field.table }}
              </span>
            </div>

            <!-- Description -->
            <p class="text-sm text-slate-600 mb-3">{{ field.description }}</p>

            <!-- Examples -->
            <div>
              <p class="text-xs font-medium text-slate-700 mb-1">Examples:</p>
              <div class="flex flex-wrap gap-1">
                <span
                  v-for="example in field.examples"
                  :key="example"
                  class="text-xs bg-white border border-slate-200 px-2 py-1 rounded font-mono text-slate-600"
                >
                  {{ example }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Usage Tips -->
        <div class="p-4 bg-amber-50 border border-amber-200 rounded-lg">
          <h4 class="text-sm font-semibold text-amber-800 mb-2 flex items-center">
            <span class="mr-2">💡</span>
            Usage Tips
          </h4>
          <ul class="space-y-1 text-xs text-amber-700">
            <li>• Click on field names to copy them</li>
            <li>• Use exact field names in your queries</li>
            <li>• Combine multiple fields for complex queries</li>
            <li>• Check data types for proper filtering</li>
          </ul>
        </div>
      </div>
    </Transition>
  </div>
</template>