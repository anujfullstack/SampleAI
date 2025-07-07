<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { savedQueriesService, type SavedQuery } from '../services/savedQueriesService'

interface Emits {
  (e: 'select-query', query: string): void
}

const emit = defineEmits<Emits>()

const lastAskedQuery = ref('')
const searchTerm = ref('')
const isSearching = ref(false)
const sampleQueries = ref<SavedQuery[]>([])
const loading = ref(false)
const selectedCategory = ref('all')

// Available categories
const categories = computed(() => {
  const uniqueCategories = [...new Set(sampleQueries.value.map(q => q.category).filter(Boolean))]
  return ['all', ...uniqueCategories]
})

const filteredQueries = computed(() => {
  let filtered = sampleQueries.value

  // Filter by category
  if (selectedCategory.value !== 'all') {
    filtered = filtered.filter(q => q.category === selectedCategory.value)
  }

  // Filter by search term
  if (searchTerm.value.trim()) {
    const search = searchTerm.value.toLowerCase()
    filtered = filtered.filter(q => 
      q.label.toLowerCase().includes(search) ||
      q.nqlQuestion.toLowerCase().includes(search)
    )
  }

  return filtered.sort((a, b) => b.useCount - a.useCount) // Sort by popularity
})

const loadSampleQueries = async () => {
  try {
    loading.value = true
    const queries = await savedQueriesService.getSampleQueries({
      limit: 100 // Get more sample queries
    })
    sampleQueries.value = queries
  } catch (error) {
    console.error('Failed to load sample queries:', error)
    // Fallback to hardcoded queries if API fails
    sampleQueries.value = []
  } finally {
    loading.value = false
  }
}

const selectQuery = async (query: SavedQuery) => {
  lastAskedQuery.value = query.nqlQuestion
  
  try {
    // Update usage count for sample queries
    await savedQueriesService.updateUsage(query.id)
    query.useCount++
  } catch (error) {
    console.error('Failed to update sample query usage:', error)
  }
  
  emit('select-query', query.nqlQuestion)
}

const clearLastAsked = () => {
  lastAskedQuery.value = ''
}

const handleSearch = () => {
  isSearching.value = true
  setTimeout(() => {
    isSearching.value = false
  }, 300)
}

// Lifecycle
onMounted(() => {
  loadSampleQueries()
})
</script>

<template>
  <div class="sticky top-24 space-y-4">
    <!-- Last Asked Section -->
    <div v-if="lastAskedQuery" class="card">
      <div class="flex items-center justify-between mb-3">
        <h3 class="text-lg font-semibold text-slate-800 flex items-center">
          <svg class="w-4 h-4 mr-2 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          Last Asked
        </h3>
        <button 
          @click="clearLastAsked"
          class="text-slate-400 hover:text-slate-600 transition-colors"
          title="Clear last asked"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>
      </div>
      <div class="p-3 bg-green-50 border border-green-200 rounded-lg">
        <p class="text-sm text-green-800 font-medium mb-2">✅ Recently Executed:</p>
        <p class="text-xs text-green-700 font-mono bg-white/60 p-2 rounded border line-clamp-2">
          "{{ lastAskedQuery }}"
        </p>
        <button 
          @click="emit('select-query', lastAskedQuery)"
          class="mt-2 text-xs text-green-600 hover:text-green-700 font-medium flex items-center"
        >
          <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
          </svg>
          Run Again
        </button>
      </div>
    </div>

    <!-- Sample Queries Section -->
    <div class="card">
      <div class="mb-4">
        <h3 class="text-lg font-semibold text-slate-800 mb-2 flex items-center">
          <svg class="w-4 h-4 mr-2 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          Sample Queries
        </h3>
        
        <!-- NQL Info Banner -->
        <div class="p-3 bg-blue-50 border border-blue-200 rounded-lg mb-3">
          <p class="text-sm text-blue-800">
            <strong>💡 NQL is for Participants only</strong><br>
            These queries work with participant data. Click any query to try it instantly.
          </p>
        </div>
        
        <!-- Category Filter -->
        <div class="flex flex-wrap gap-2 mb-3">
          <button
            v-for="category in categories"
            :key="category"
            @click="selectedCategory = category"
            class="text-xs px-3 py-1 rounded-lg transition-all capitalize"
            :class="selectedCategory === category 
              ? 'bg-blue-500 text-white' 
              : 'bg-slate-100 text-slate-700 hover:bg-blue-100'"
          >
            {{ category === 'all' ? 'All Categories' : category }}
          </button>
        </div>
        
        <!-- Search Box -->
        <div class="relative">
          <input
            v-model="searchTerm"
            @input="handleSearch"
            type="text"
            placeholder="Search queries..."
            class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none pl-8"
          />
          <svg v-if="!isSearching" class="w-4 h-4 text-slate-400 absolute left-2.5 top-2.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
          </svg>
          <div v-else class="absolute left-2.5 top-2.5">
            <div class="loading-spinner w-4 h-4 border-slate-400"></div>
          </div>
        </div>
      </div>

      <!-- Query List -->
      <div class="space-y-2 max-h-96 overflow-y-auto">
        <div v-if="loading" class="text-center py-8">
          <div class="loading-spinner mx-auto mb-2"></div>
          <p class="text-sm text-slate-600">Loading sample queries...</p>
        </div>

        <div v-else-if="filteredQueries.length === 0" class="text-center py-8 text-slate-500">
          <svg class="w-12 h-12 mx-auto mb-3 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          <p class="text-sm">No queries found</p>
          <p class="text-xs text-slate-400 mt-1">Try a different search term or category</p>
        </div>

        <div
          v-for="query in filteredQueries"
          :key="query.id"
          @click="selectQuery(query)"
          class="group p-3 bg-slate-50 hover:bg-blue-50 rounded-lg border border-slate-200 hover:border-blue-300 cursor-pointer transition-all duration-200 hover:shadow-sm"
        >
          <div class="flex items-start justify-between">
            <div class="flex-1 min-w-0 pr-2">
              <div class="flex items-center gap-2 mb-1">
                <h5 class="text-sm font-medium text-slate-800 group-hover:text-blue-700 transition-colors">
                  {{ query.label }}
                </h5>
                <span v-if="query.category" class="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full">
                  {{ query.category }}
                </span>
              </div>
              <p class="text-xs text-slate-600 group-hover:text-blue-600 transition-colors leading-relaxed line-clamp-2">
                {{ query.nqlQuestion }}
              </p>
              <div v-if="query.useCount > 0" class="flex items-center mt-1 text-xs text-slate-500">
                <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"/>
                </svg>
                Used {{ query.useCount }} times
              </div>
            </div>
            <div class="flex-shrink-0">
              <svg class="w-4 h-4 text-slate-400 group-hover:text-blue-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
              </svg>
            </div>
          </div>
        </div>
      </div>

      <!-- Query Count -->
      <div class="mt-4 pt-3 border-t border-slate-200">
        <div class="flex items-center justify-between text-xs text-slate-500">
          <span>{{ filteredQueries.length }} of {{ sampleQueries.length }} queries</span>
          <div class="flex items-center space-x-1">
            <div class="w-2 h-2 bg-green-400 rounded-full"></div>
            <span>Ready to use</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Tips -->
    <div class="card">
      <h4 class="text-sm font-semibold text-slate-800 mb-3 flex items-center">
        <span class="mr-2">💡</span>
        Quick Tips
      </h4>
      <ul class="space-y-2 text-xs text-slate-600">
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Sample queries are loaded from the database</span>
        </li>
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Popular queries appear first</span>
        </li>
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Use categories to find specific types of queries</span>
        </li>
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Save your successful queries to the repository</span>
        </li>
        <li class="flex items-start">
          <span class="text-blue-500 mr-2">•</span>
          <span>Give feedback with thumbs up/down</span>
        </li>
      </ul>
    </div>
  </div>
</template>